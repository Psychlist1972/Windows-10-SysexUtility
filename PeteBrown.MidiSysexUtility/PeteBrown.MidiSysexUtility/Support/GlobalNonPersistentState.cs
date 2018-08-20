using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeteBrown.MidiSysexUtility
{
    class GlobalNonPersistentState
    {
        public static bool IsCurrentlyActiveApp { get; set; }
    }
}
