using System;
using System.Collections.Generic;
using System.Reflection;

namespace ManeuverNodeSplitter.KER
{
    class KERWrapper
    {
        private static IKerbalEngineerWrapper wrapper;
        public static bool Available {
            get {
                return Instance != null;
            }
        }
        public static IKerbalEngineerWrapper Instance {
            get {
                if(wrapper == null)
                {
                    Type type = ToolbarTypes.getType("KerbalEngineer.Flight.Readouts.Vessel.SimulationProcessor");
                    if(type != null)
                    {
                        object processor = ToolbarTypes.getStaticProperty(type, "Instance").GetValue(null, null);
                        wrapper = new KerbalEngineerWrapper(processor);
                    }
                }
                return wrapper;
            }
        }
    }

    interface IKerbalEngineerWrapper
    {
        Stage[] GetStages();
    }

    class Stage
    {
        private object stage;

        //private FieldInfo number;
        //private FieldInfo deltaV;
        //private FieldInfo time;
        //private FieldInfo totalMass;
        //private FieldInfo isp;
        //private FieldInfo thrust;

        //public int Number { get { return (int) number.GetValue(stage); } }
        //public double DeltaV { get { return (double) deltaV.GetValue(stage); } }
        //public double Time { get { return (double) time.GetValue(stage); } }
        //public double TotalMass { get { return (double) totalMass.GetValue(stage); } }
        //public double Isp { get { return (double) isp.GetValue(stage); } }
        //public double Thrust { get { return (double) thrust.GetValue(stage); } }

        /* Holding onto the FieldInfo and retrieving the values lazily is causing KSP to crash,
         * but only if one of the maneuvers other than the final maneuver will eject the vessel
         * from the parent SOI instead of ejection happening with the final maneuver (I suspect
         * KER is doing something with the stage object which we reference from here in that
         * situation), so we eagerly fetch and store the actual values here instead.
         */
        public int Number { get; private set; }
        public double DeltaV { get; private set; }
        public double Time { get; private set; }
        public double TotalMass { get; private set; }
        public double Isp { get; private set; }
        public double Thrust { get; private set; }

        internal Stage(object stage)
        {
            this.stage = stage;

            //number = stage.GetType().GetField("number", BindingFlags.Public | BindingFlags.Instance);
            //deltaV = stage.GetType().GetField("deltaV", BindingFlags.Public | BindingFlags.Instance);
            //time = stage.GetType().GetField("time", BindingFlags.Public | BindingFlags.Instance);
            //totalMass = stage.GetType().GetField("totalMass", BindingFlags.Public | BindingFlags.Instance);
            //isp = stage.GetType().GetField("isp", BindingFlags.Public | BindingFlags.Instance);
            //thrust = stage.GetType().GetField("thrust", BindingFlags.Public | BindingFlags.Instance);

            Number = (int) stage.GetType().GetField("number", BindingFlags.Public | BindingFlags.Instance).GetValue(stage);
            DeltaV = (double) stage.GetType().GetField("deltaV", BindingFlags.Public | BindingFlags.Instance).GetValue(stage);
            Time = (double) stage.GetType().GetField("time", BindingFlags.Public | BindingFlags.Instance).GetValue(stage);
            TotalMass = (double) stage.GetType().GetField("totalMass", BindingFlags.Public | BindingFlags.Instance).GetValue(stage);
            Isp = (double) stage.GetType().GetField("isp", BindingFlags.Public | BindingFlags.Instance).GetValue(stage);
            Thrust = (double) stage.GetType().GetField("thrust", BindingFlags.Public | BindingFlags.Instance).GetValue(stage);
        }
    }

    class KerbalEngineerWrapper : IKerbalEngineerWrapper
    {
        private object processor;
        private PropertyInfo stages;

        internal KerbalEngineerWrapper(object processor)
        {
            this.processor = processor;

            stages = processor.GetType().GetProperty("Stages", BindingFlags.Public | BindingFlags.Static);
        }

        public Stage[] GetStages()
        {
            Array raw = (Array) stages.GetValue(null, null);
            Stage[] array = new Stage[raw.Length];
            for(int index = 0; index < raw.Length; index += 1)
            {
                array[index] = new Stage(raw.GetValue(index));
            }
            return array;
        }
    }
}
