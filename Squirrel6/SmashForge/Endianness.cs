using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashForge {

    // System.IO.Endianness??????
    public class Endianness {
        public static readonly Endianness Little = new Endianness("Little");
        public static readonly Endianness Big = new Endianness("Big");
        private string v;

        public Endianness(string v) {
            this.v = v;
        }

        public override string ToString() {
            return v;
        }
    }
}
