/// Copyright (c) 2012 Ecma International.  All rights reserved. 
/**
 * @path ch15/15.2/15.2.3/15.2.3.7/15.2.3.7-5-b-74.js
 * @description Object.defineProperties - 'descObj' is a String object which implements its own [[Get]] method to get 'configurable' property (8.10.5 step 4.a)
 */


function testcase() {

        var obj = {};

        var descObj = new String();
        descObj.configurable = true;

        Object.defineProperties(obj, {
            prop: descObj
        });
        var result1 = obj.hasOwnProperty("prop");
        delete obj.prop;
        var result2 = obj.hasOwnProperty("prop");

        return result1 === true && result2 === false;
    }
runTestCase(testcase);
