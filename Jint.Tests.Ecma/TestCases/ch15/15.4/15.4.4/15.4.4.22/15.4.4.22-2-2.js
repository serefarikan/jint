/// Copyright (c) 2012 Ecma International.  All rights reserved. 
/**
 * @path ch15/15.4/15.4.4/15.4.4.22/15.4.4.22-2-2.js
 * @description Array.prototype.reduceRight - 'length' is own data property on an Array
 */


function testcase() {

        var accessed = false;

        function callbackfn(prevVal, curVal, idx, obj) {
            accessed = true;
            return obj.length === 2;
        }

        return [12, 11].reduceRight(callbackfn, 11) && accessed;
    }
runTestCase(testcase);
