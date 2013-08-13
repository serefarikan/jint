﻿using System;
using System.Collections.Generic;
using Jint.Native.Errors;
using Jint.Native.String;
using Jint.Runtime;
using Jint.Runtime.Descriptors;

namespace Jint.Native.Object
{
    public class ObjectInstance
    {
        public ObjectInstance(ObjectInstance prototype)
        {
            Properties = new Dictionary<string, PropertyDescriptor>();
            Extensible = true;
            Prototype = prototype;
            DefineOwnProperty("prototype", new DataDescriptor(prototype), false);
        }

        public IDictionary<string, PropertyDescriptor> Properties { get; private set; }

        /// <summary>
        /// The prototype of this object.
        /// </summary>
        public ObjectInstance Prototype { get; private set; }

        
        /// <summary>
        /// If true, own properties may be added to the 
        /// object.
        /// </summary>
        public bool Extensible { get; set; }

        /// <summary>
        /// A String value indicating a specification defined 
        /// classification of objects.
        /// </summary>
        public virtual string Class
        {
            get { return "Object"; }
        }

        /// <summary>
        /// Returns the value of the named property.
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.12.3
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public virtual object Get(string propertyName)
        {
            var desc = GetProperty(propertyName);

            if (desc == PropertyDescriptor.Undefined)
            {
                return Undefined.Instance;
            }

            if (desc.IsDataDescriptor())
            {
                return desc.As<DataDescriptor>().Value;
            }

            var getter = desc.As<AccessorDescriptor>().Get;

            return getter.Call(this, null);
        }

        public void Set(string name, object value)
        {

            if (!HasProperty(name))
            {
                DefineOwnProperty(name, new DataDescriptor(value) { Configurable = true, Enumerable = true, Writable = true }, false);
            }
            else
            {
                Put(name, value, false);
            }
        }
        /// <summary>
        /// Returns the Property Descriptor of the named 
        /// own property of this object, or undefined if 
        /// absent.
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.12.1
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public PropertyDescriptor GetOwnProperty(string propertyName)
        {
            PropertyDescriptor x;
            if (Properties.TryGetValue(propertyName, out x))
            {
                PropertyDescriptor d;
                if (x.IsDataDescriptor())
                {
                    d = new DataDescriptor(x.As<DataDescriptor>());
                }
                else
                {
                    d = new AccessorDescriptor(x.As<AccessorDescriptor>());
                }

                return d;
            }
            
            return PropertyDescriptor.Undefined;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.12.2
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public PropertyDescriptor GetProperty(string propertyName)
        {
            var prop = GetOwnProperty(propertyName);

            if (prop != PropertyDescriptor.Undefined)
            {
                return prop;
            }
            
            if(Prototype == null)
            {
                return PropertyDescriptor.Undefined;
            }

            return Prototype.GetProperty(propertyName);
        }

        /// <summary>
        /// Sets the specified named property to the value 
        /// of the second parameter. The flag controls 
        /// failure handling.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <param name="throwOnError"></param>
        public void Put(string propertyName, object value, bool throwOnError)
        {
            if (!CanPut(propertyName))
            {
                if (throwOnError)
                {
                    throw new TypeError();
                }

                return;
            }

            var ownDesc = GetOwnProperty(propertyName);

            if (ownDesc.IsDataDescriptor())
            {
                var valueDesc = new DataDescriptor(value);
                DefineOwnProperty(propertyName, valueDesc, throwOnError);
                return;
            }

            var desc = GetProperty(propertyName);

            if (desc.IsAccessorDescriptor())
            {
                var setter = desc.As<AccessorDescriptor>().Set;
                setter.Call(this, new [] {value});
            }
            else
            {
                var newDesc = new DataDescriptor(value) {Writable = true, Enumerable = true, Configurable = true};
                DefineOwnProperty(propertyName, newDesc, throwOnError);
            }
        }

        /// <summary>
        /// Returns a Boolean value indicating whether a 
        /// [[Put]] operation with PropertyName can be 
        /// performed.
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-8.12.4
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool CanPut(string propertyName)
        {
            var desc = GetOwnProperty(propertyName);

            if (desc != PropertyDescriptor.Undefined)
            {
                if (desc.IsAccessorDescriptor())
                {
                    if (desc.As<AccessorDescriptor>().Set == null)
                    {
                        return false;
                    }

                    return true;
                }
                
                return desc.As<DataDescriptor>().Writable;
            }

            if (Prototype == null)
            {
                return Extensible;
            }

            var inherited = Prototype.GetProperty(propertyName);

            if (inherited == PropertyDescriptor.Undefined)
            {
                return Prototype.Extensible;
            }

            if (inherited.IsAccessorDescriptor())
            {
                if (inherited.As<AccessorDescriptor>().Set == null)
                {
                    return false;
                }

                return true;
            }

            if (!Extensible)
            {
                return false;
            }
            else
            {
                return inherited.As<DataDescriptor>().Writable;
            }
        }

        /// <summary>
        /// Returns a Boolean value indicating whether the 
        /// object already has a property with the given 
        /// name.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool HasProperty(string propertyName)
        {
            return GetProperty(propertyName) != PropertyDescriptor.Undefined;
        }

        /// <summary>
        /// Removes the specified named own property 
        /// from the object. The flag controls failure 
        /// handling.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public bool Delete(string propertyName, bool throwOnError)
        {
            var desc = GetOwnProperty(propertyName);
            
            if (desc == PropertyDescriptor.Undefined)
            {
                return true;
            }

            if (desc.Configurable)
            {
                Properties.Remove(propertyName);
                return true;
            }
            else
            {
                if (throwOnError)
                {
                    throw new TypeError();
                }

                return false;
            }
        }

        /// <summary>
        /// Hint is a String. Returns a default value for the 
        /// object.
        /// </summary>
        /// <param name="hint"></param>
        /// <returns></returns>
        public object DefaultValue(TypeCode hint)
        {
            if ((hint == TypeCode.String) || (hint == TypeCode.Empty && this is StringInstance))
            {
                var toString = this.Get("toString");
                var callable = toString as ICallable;
                if (callable != null)
                {
                    var str = callable.Call(this, Arguments.Empty);
                    if (str is IPrimitiveType)
                    {
                        return str;
                    }
                }

                var valueOf = this.Get("valueOf");
                callable = valueOf as ICallable;
                if (callable != null)
                {
                    var val = callable.Call(this, Arguments.Empty);
                    if (val is IPrimitiveType)
                    {
                        return val;
                    }
                }

                throw new TypeError();
            }

            if ((hint == TypeCode.Double) || (hint == TypeCode.Empty))
            {
                var valueOf = this.Get("valueOf");
                var callable = valueOf as ICallable;
                if (callable != null)
                {
                    var val = callable.Call(this, Arguments.Empty);
                    if (val is IPrimitiveType)
                    {
                        return val;
                    }
                }

                var toString = this.Get("toString");
                callable = toString as ICallable;
                if (callable != null)
                {
                    var str = callable.Call(this, Arguments.Empty);
                    if (str is IPrimitiveType)
                    {
                        return str;
                    }
                }

                throw new TypeError();
            }

            return ToString();
        }

        /// <summary>
        /// Creates or alters the named own property to 
        /// have the state described by a Property 
        /// Descriptor. The flag controls failure handling.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="desc"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public bool DefineOwnProperty(string propertyName, PropertyDescriptor desc, bool throwOnError)
        {
            var current = GetOwnProperty(propertyName);
            
            if (current == PropertyDescriptor.Undefined)
            {
                if (!Extensible)
                {
                    if (throwOnError)
                    {
                        throw new TypeError();
                    }

                    return false;
                }
                else
                {
                    if (desc.IsGenericDescriptor() || desc.IsDataDescriptor())
                    {
                        Properties.Add(propertyName, new DataDescriptor(desc.As<DataDescriptor>()));
                    }
                    else
                    {
                        Properties.Add(propertyName, new AccessorDescriptor(desc.As<AccessorDescriptor>()));
                    }
                }

                return true;
            }

            // todo: if desc and current are the same, return true

            if (!current.Configurable)
            {
                if (desc.Configurable)
                {
                    if (throwOnError)
                    {
                        throw new TypeError();
                    }

                    return false;
                }

                if (desc.Enumerable != current.Enumerable)
                {
                    if (throwOnError)
                    {
                        throw new TypeError();
                    }

                    return false;
                }
            }

            if (desc.IsGenericDescriptor())
            {
                // ????
            }

            if (current.IsDataDescriptor() != desc.IsDataDescriptor())
            {
                if (!current.Configurable)
                {
                    if (throwOnError)
                    {
                        throw new TypeError();
                    }

                    return false;
                }

                if (current.IsDataDescriptor())
                {
                    // todo: convert to accessor
                }
            }
            else if (current.IsDataDescriptor() && desc.IsDataDescriptor())
            {
                var cd = current.As<DataDescriptor>();
                var dd = current.As<DataDescriptor>();

                if (!current.Configurable)
                {
                    if (!cd.Writable && dd.Writable)
                    {
                        if (throwOnError)
                        {
                            throw new TypeError();
                        }

                        return false;
                    }
                }
            }
            else if (current.IsAccessorDescriptor() && desc.IsAccessorDescriptor())
            {
                var ca = current.As<AccessorDescriptor>();
                var da = current.As<AccessorDescriptor>();

                if (!current.Configurable)
                {
                    if ( (da.Set != null && da.Set != ca.Set)
                        || (da.Get != null && da.Get != ca.Get))
                    {
                        if (throwOnError)
                        {
                            throw new TypeError();
                        }

                        return false;
                    }
                }
            }

            Properties[propertyName] = desc;

            return true;
        }
    }
}
