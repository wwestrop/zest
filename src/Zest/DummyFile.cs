using System;

namespace Zest {

    class DummyFile {

        public int Prop { get; private set; }

        //public int NoSetter { get { return 5; } }

        public int MyFunc(object a, byte b, char c, DummyFile d)
        {
            return 42;
        }

        public void foobar() {
            var d = DateTime.Now;
        }

        public DummyFile self() { return this; }

    }


    internal class MySingleton {
        
        internal static MySingleton Instance { get; private set; }
        
        private MySingleton() {
            Instance = this;
        }

    }

    internal static class MyDateTime {
        private static DateTime _cachedValue = new DateTime(2015, 11, 30);


        public static DateTime Now1 => _cachedValue;

        public static DateTime Now2 {
            get {
                return _cachedValue;                // OH NOES! What if we want a diff. value returned for testing purposes?
            }
        }

        // could rewrite properties as get_ and set_ method pairs (how they are internally compiled anyway, so ABI shouldn't break)
    }

}
