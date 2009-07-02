﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using OpenMetaverse;
using org.opencyc.api;
using org.opencyc.cycobject;
using CUID = org.opencyc.util.UUID;
//using Guid=org.opencyc.cycobject.Guid;
//using CycFort = org.opencyc.cycobject.CycObject;
using OpenMetaverse.Assets;
using PathSystem3D.Navigation;
using Exception=System.Exception;
using Object=System.Object;

namespace CycWorldModule.DotCYC
{

    public class SimCyclifier : SimEventSubscriber
    {
        static private Thread EventQueueHandler;
        static readonly Queue<SimObjectEvent> EventQueue = new Queue<SimObjectEvent>();
        static readonly List<String> SkipVerbs = new List<string>() { "on-log-message", "on-login", "on-event-queue-running", "on-sim-connecting" };

        static SimCyclifier Master;

        public void OnEvent(SimObjectEvent evt)
        {
            if (evt.EventType==SimEventType.NETWORK) return;
            if (SkipVerbs.Contains(evt.Verb.ToLower())) return;
            lock (EventQueue)
            {
                EventQueue.Enqueue(evt);
            }
        }

        static void EventQueue_Handler()
        {
            SimObjectEvent evt = null;
            while(true)
            {
               // int eventQueueCountLast = 0;
                int eventQueueCount = 0;
                lock (EventQueue)
                {
                    eventQueueCount = EventQueue.Count;
                    if (eventQueueCount > 0)
                    {
                        evt = EventQueue.Dequeue();
                    } else
                    {
                        evt = null;
                    }
                }
                if (eventQueueCount > 250)
                {
                    Debug("eventQueueCount=" + eventQueueCount);
                }
                if (evt!=null)
                {
                    OnEvent0(evt);
                } else
                {
                    Thread.Sleep(500);
                }
               // eventQueueCountLast = eventQueueCount;
            }
        }

        static public void OnEvent0(SimObjectEvent evt)
        {
            try
            {
                CycFort fort = Master.FindOrCreateCycFort(evt);
               // Debug("to fort -> " + fort);
            }
            catch (Exception e)
            {

                Exception(e);
                Debug("Error to cyc -> " + evt + " " + e);
            }

        }

        public void ShuttingDown()
        {
            throw new NotImplementedException();
        }

        public static bool UseCyc = true;
        public static bool ClearKBBetweenSessions = false;
        static public CycAccess cycAccess;
        static public CycFort vocabMt;
        static public CycFort assertMt;
        static public Dictionary<string, CycFort> simFort = new Dictionary<string, CycFort>();
        static CycConnectionForm cycConnection;
        static readonly DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0);

        public void assertGenls(CycFort a, CycFort b)
        {
            try
            {
                cycAccess.assertGenls(a, b, vocabMt);
            }
            catch (Exception e)
            {

                Exception(e);
            }
        }

        public void assertIsa(CycFort a, CycFort b)
        {
            try
            {
                cycAccess.assertIsa(a, b, vocabMt);
            }
            catch (Exception e)
            {

                Exception(e);
            }
        }

        static object SimCyclifierLock = new object();

        public SimCyclifier(CycWorldModule tf)
        {
            lock (SimCyclifierLock)
            {
                if (Master == null) Master = this;
                if (!UseCyc) return;
                if (cycConnection == null)
                {
                    cycConnection = tf.CycConnectionForm;
                    AssertKE();
                }
                if (EventQueueHandler == null)
                {
                    EventQueueHandler = new Thread(EventQueue_Handler);
                    EventQueueHandler.Start();
                }
            }
        }

        private void AssertKE()
        {

            cycAccess = cycConnection.getCycAccess();

            assertMt = createIndividual("SimDataMt", "#$DataMicrotheory for the simulator", "UniversalVocabularyMt",
                                        "DataMicrotheory");
            vocabMt = createIndividual("SimVocabMt", "#$VocabularyMicrotheory for the simulator",
                                       "UniversalVocabularyMt",
                                       "VocabularyMicrotheory");


            if (ClearKBBetweenSessions || true)
            {
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-LISPFn\"))");
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-EFFECTFn\"))");
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-ANIMFn\"))");
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-UNKNOWNFn\"))");
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-NETWORKFn\"))");
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-SOCIALFn\"))");
                cycAccess.converseVoid("(fi-kill (find-or-create-constant \"SimEvent-MOVEMENTFn\"))");
            }
            simFort["SimObject"] = createIndividual("SimObject", "#$SpatiallyDisjointObjectType for the simulator",
                                                    "SimVocabMt", "Collection");
            simFort["SimAsset"] = createIndividual("SimAsset",
                                                   "An SimAsset is a #$PartiallyTangibleTypeByPhysicalFeature from the simulator such as #$BlueColor or #$BlowingAKiss animation",
                                                   "SimVocabMt", "Collection");
            simFort["SimAvatar"] = createIndividual("SimAvatar", "#$Agent-Generic for the simulator", "SimVocabMt",
                                                    "Collection");

            assertGenls(simFort["SimAvatar"], simFort["SimObject"]);
            FunctionToIndividual("SimRegionFn", "SimRegion", "A region in the simulator");
            FunctionToIndividual("SimAvatarFn", "SimAvatar", "An avatar in the simulator");
            FunctionToIndividual("SimObjectFn", "SimObject", "A primitive in the simulator");
            assertIsa(simFort["SimObject"], C("SpatiallyDisjointObjectType"));

            // FunctionToCollection("SimAnimationFn", "SimAnimation", "An animation in the simulator");

            assertIsa(C("simEventData"), C("VariableArityPredicate"));
            assertIsa(C("SimObjectEvent"), C("Collection"));
            assertGenls(C("SimObjectEvent"), C("Event"));
            FunctionToCollection("SimAssetFn", "SimAsset", "Simulator assets that denote a feature set");
            
            
            // visit libomv
            if (cycAccess.find("SimEnumCollection")==null)
            {
                Debug("Loading SimEnumCollection Collections ");
                assertIsa(C("SimEnumCollection"), C("Collection"));
                assertGaf(C("comment"), C("SimEnumCollection"), "Enums collected from SecondLife");
                VisitAssembly(Assembly.GetAssembly(typeof(AssetType)));
            }
            else
            {
                Debug("Found SimEnumCollection Collections ");
            }
            if (cycAccess.find("SimEventType-SCRIPT") == null)
            {
                VisitAssembly(Assembly.GetAssembly(typeof (SimEventType)));
            }

            simFort["SimRegion"] = cycAccess.createCollection("SimRegion", "A region in the simulator", vocabMt, C("Collection"),
                           C("GeographicalPlace-3D"));
            assertGenls(simFort["SimRegion"], C("Polyhedron"));


            assertGenls(C("BodyMovementEvent"), C("SimAnimation"));
            conceptuallyRelated(C("EmittisgSound"), C("SimSound"));
            conceptuallyRelated(C("Sound"), C("SimSound"));
            conceptuallyRelated(C("AnimalBodyRegion"), C("SimBodypart"));
            conceptuallyRelated(C("SomethingToWear"), C("SimWearable"));
            conceptuallyRelated(C("Landmark"), C("SimLandmark"));
            conceptuallyRelated(C("VisualImage"), C("SimTexture"));



            bool newlyCreated;
            createIndividual("SimGlobalMapCoordinateSystem", "the secondlife global coordinate system", "CartesianCoordinateSystem", out newlyCreated);


            cycAccess.createCollection("SimRegionMapCoordinateSystem",
                                       "instances are secondlife regional coordinate systems", vocabMt, C("Collection"),
                                       C("ThreeDimensionalCoordinateSystem"));

            ResultIsa("SimRegionCoordinateSystemFn", "SimRegionMapCoordinateSystem",
                "#$SimRegionCoordinateSystemFn Takes a #$SimRegion and returns a example: (#$SimRegionCoordinateSystemFn (#$SimRegionFn \"LogicMoo\")) => #$ThreeDimensionalCoordinateSystem",
                simFort["SimRegion"]);

            createIndividual("PointInRegionFn", "Creates region 3D #$Point relative to (#$SimRegionFn :ARG1)", "SimVocabMt", "QuaternaryFunction");
            assertIsa(C("PointInRegionFn"), C("TotalFunction"));
            //assertGaf(C("isa"), simFort["PointInRegionFn"], C(""));
            assertGaf(C("arg1Isa"), simFort["PointInRegionFn"], C("IDString"));
            assertGaf(C("arg2Isa"), simFort["PointInRegionFn"], C("NumericInterval"));
            assertGaf(C("arg3Isa"), simFort["PointInRegionFn"], C("NumericInterval"));
            assertGaf(C("arg4Isa"), simFort["PointInRegionFn"], C("NumericInterval"));
            assertGaf(C("resultIsa"), simFort["PointInRegionFn"], C("Point"));
            cycAssert("(#$expansion #$PointInRegionFn "
                + " (#$PointIn3DCoordinateSystemFn (#$SimRegionCoordinateSystemFn"
                + " (#$SimRegionFn :ARG1)) :ARG2 :ARG3 :ARG4 ))");

            createIndividual("PointRelativeToFn", "Creates Local 3D #$Point relative to the #$SpatialThing-Localized in :ARG1", "SimVocabMt", "QuaternaryFunction");
            assertIsa(C("PointRelativeToFn"), C("QuaternaryFunction"));
            assertIsa(C("PointRelativeToFn"), C("TotalFunction"));
            assertGaf(C("arg1Isa"), simFort["PointRelativeToFn"], C("SpatialThing-Localized"));
            assertGaf(C("arg2Isa"), simFort["PointRelativeToFn"], C("NumericInterval"));
            assertGaf(C("arg3Isa"), simFort["PointRelativeToFn"], C("NumericInterval"));
            assertGaf(C("arg4Isa"), simFort["PointRelativeToFn"], C("NumericInterval"));
            assertGaf(C("resultIsa"), C("PointRelativeToFn"), C("Point"));


            cycAssert("(#$pointInSystem (#$PointInRegionFn ?STR ?X ?Y ?Z) (#$SimRegionCoordinateSystemFn (#$SimRegionFn ?STR)))");
            cycAssert("(#$pointInSystem (#$PointInRegionFn \"Daxlandia\" 128 120 27) (#$SimRegionCoordinateSystemFn (#$SimRegionFn \"Daxlandia\")))");

        }

        private void VisitAssembly(Assembly assembly)
        {
            foreach (Type t  in assembly.GetTypes())
            {

                try
                {
                    VisitType(t);
                }
                catch (Exception e)
                {
                    Debug("" + e);
                }
            }

        }

        static void Debug(string s)
        {
           Console.WriteLine(s);
        }

        static void Exception(Exception e)
        {
            Debug("" + e.Message + "\n" + e.StackTrace);
        }

        public bool cycAssert(string s)
        {
            try
            {
                return cycAccess.converseBoolean("(fi-assert '" + s + " " + vocabMt.cyclifyWithEscapeChars() + ")");
            }
            catch (Exception e)
            {

                Exception(e);
                return false;
            }
        }

        public void ResultIsa(string fn, string col, string comment, CycFort arg1Isa)
        {
            simFort[fn] = createIndividual(fn, comment, "SimVocabMt", "ReifiableFunction");
            assertGaf(C("resultIsa"), simFort[fn], C(col));
            assertGaf(C("arg1Isa"), simFort[fn], arg1Isa);

        }

        public void conceptuallyRelated(CycFort a, CycFort b)
        {
            assertGaf(C("conceptuallyCoRelated"), a, b);
        }

        public void VisitType(Type type)
        {
            if (type.IsSubclassOf(typeof(Asset)))
            {
                string fn = type.Name;
                if (fn.StartsWith("Asset")) fn = fn.Substring(5);
                string col = "Sim" + fn;
                string comment =
                    "A spec of #$SimAsset that is a #$PartiallyTangibleTypeByPhysicalFeature from the simulator's type " +
                    type.FullName + " such as #$BlueColor or #$BlowingAKiss animation";
                fn = "Sim" + fn + "Fn";
                FunctionToCollection(fn, col, comment);
                assertGaf(C("genlFuncs"), simFort[fn], simFort["SimAssetFn"]);
                assertGenls(simFort[col], simFort["SimAsset"]);
            }
            else //if (type.Namespace ==null || type.Namespace.StartsWith("OpenMetaverse"))
            {
                try
                {
                    if (type.IsEnum) VisitEnumType(type);
                    else
                    {
                        
                    }
                }
                catch (Exception e)
                {
                    Debug("" + e);
                }
            }
        }

        static readonly Dictionary<Type, CycFort> typeFort = new Dictionary<Type, CycFort>();

        private CycFort VisitEnumType(Type type)
        {
            lock (typeFort)
            {
                CycFort cn;
                if (typeFort.TryGetValue(type, out cn)) return cn;
                string name = "Sim" + type.Name;
                if (name.StartsWith("SimSim"))
                {
                    name = name.Substring(3);
                }
                cn = C(name);
                assertIsa(cn, C("Collection"));
                assertIsa(cn, C("SimEnumCollection"));
                assertGaf(C("comment"), cn, "The sim enum for " + type);
                typeFort[type] = cn;
                if (type.IsEnum)
                {
                    foreach (FieldInfo fort in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        string v = type.Name + "-" + fort.Name;
                        CycFort cv = C(v);
                        assertIsa(cv, C("Collection"));
                        assertGaf(C("genls"),cv, cn);
                        assertGaf(C("comment"), cv, "The sim enum value for " + fort);
                    }
                }
                return null;
            }
        }

        public void FunctionToCollection(string fn, string col, string comment)
        {
            simFort[fn] = createIndividual(fn, comment, "SimVocabMt", "UnaryFunction");
            assertIsa(simFort[fn], C("CollectionDenotingFunction"));
            assertIsa(simFort[fn], C("ReifiableFunction"));
            simFort[col] = createIndividual(col, comment, "SimVocabMt", "Collection");
            assertIsa(simFort[col], C("PartiallyTangibleTypeByPhysicalFeature"));
            //not true assertIsa(simFort[fn], C("SubcollectionDenotingFunction"));
            //not true assertIsa(simFort[fn], C("TotalFunction"));
            assertGaf(C("resultIsa"), simFort[fn], C("PartiallyTangibleTypeByPhysicalFeature"));
            assertGaf(C("resultIsa"), simFort[fn], C("FirstOrderCollection"));
            assertGaf(C("resultGenl"), simFort[fn], simFort[col]);
        }

        public void FunctionToIndividual(string fn, string col, string comment)
        {
            simFort[fn] = createIndividual(fn, comment, "SimVocabMt", "UnaryFunction");
            assertIsa(simFort[fn], C("IndividualDenotingFunction"));
            assertIsa(simFort[fn], C("ReifiableFunction"));
            simFort[col] = createIndividual(col, comment, "SimVocabMt", "Collection");
            assertGaf(C("resultIsa"), simFort[fn], simFort[col]);
        }

        public CycFort createIndividual(string term, string comment, string mt, string type)
        {
            bool newlyCreated;
            return createIndividual(term, comment, mt, type, out newlyCreated);
        }

        public void assertGaf(CycFort a, CycFort b, CycFort c)
        {
            try
            {
                cycAccess.assertGaf(vocabMt, a, b, c);
            }
            catch (Exception e)
            {

                Exception(e);
            }
        }

        public void assertGaf(CycFort a, CycFort b, string c)
        {
            try
            {
                cycAccess.assertGaf(vocabMt, a, b, c);
            }
            catch (Exception e)
            {

                Exception(e);
            }
        }



        public CycFort createIndividual(string term, string comment, string mt, string type, out bool created)
        {
            lock (simFort)
            {
                if (simFort.ContainsKey(term))
                {
                    created = false;
                    return simFort[term];
                }
                created = true;
                return simFort[term] = cycAccess.createIndividual(term, comment, mt, type);
            }

        }
        public CycFort createIndividual(string term, string comment, string type, out bool created)
        {
            return createIndividual(term, comment, "SimVocabMt", type, out created);

        }

        readonly static public Dictionary<object, CycFort> cycTerms = new Dictionary<object, CycFort>();


        public CycFort FindOrCreateCycFort(SimObject obj)
        {
            lock (cycTerms)
            {
                CycFort constant;
                if (cycTerms.TryGetValue(obj, out constant)) return constant;
                string name;
                string type;
                if (obj is SimAvatar)
                {
                    type = "SimAvatar";
                    name = obj.GetName();
                }
                else
                {
                    type = "SimObject";
                    name = obj.ID.ToString();
                }

                //byte[] ba = id.GetBytes();
                ////ulong umsb = Utils.BytesToUInt64(ba);
                ////long msb = umsb;
                ////long lsb = 0L;
                //System.Guid g = new System.Guid();
                ////CUID cycid = CUID.nameUUIDFromBytes(ba);
                constant = createIndividualFn(type, name, "" + obj.DebugInfo(), "SimVocabMt", type);
                cycTerms[obj] = constant;
                return constant;
            }
        }

        public CycFort FindOrCreateCycFort(SimAsset simObj)
        {
            lock (cycTerms)
            {
                CycFort constant;
                if (cycTerms.TryGetValue(simObj, out constant)) return constant;
                constant = createIndividualFn("Sim" + simObj.AssetType, simObj.Name, simObj.DebugInfo(), "SimVocabMt", "Sim" + simObj.AssetType);
                cycTerms[simObj] = constant;
                return constant;
            }
        }
        public CycFort FindOrCreateCycFort(Asset simObj)
        {
            return FindOrCreateCycFort(SimAssetStore.GetSimAsset(simObj));
        }

        public CycFort FindOrCreateCycFort(SimObjectType simObj)
        {
            lock (cycTerms)
            {
                CycFort constant;
                if (cycTerms.TryGetValue(simObj, out constant)) return constant;
                constant = cycAccess.createIndividual(simObj.AspectName, simObj.ToDebugString(), "SimVocabMt",
                                                      "SimObjectType");
                cycTerms[simObj] = constant;
                return constant;
            }
        }

        public static int TimeRep = 0;
        /// <summary>
        ///  (MilliSecondFn 34 (SecondFn 59 (MinuteFn 12 (HourFn 18 (DayFn 14 (MonthFn February (YearFn 1966))))))
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public CycFort FindOrCreateCycFort(DateTime dateTime)
        {
            switch (TimeRep)
            {
                case 0:
                    return new CycNart(makeCycList(C("DateFromStringFn"), dateTime.ToString("MMMM dd, yyyy HH:mm:ss.ffffZ")));
                case 1:
                    return new CycNart(
                        makeCycList(C("MilliSecondFn"), FindOrCreateCycFort(dateTime.Millisecond),
                                    makeCycList(C("SecondFn"), FindOrCreateCycFort(dateTime.Second),
                                                makeCycList(C("MinuteFn"), FindOrCreateCycFort(dateTime.Minute),
                                                            makeCycList(C("HourFn"), FindOrCreateCycFort(dateTime.Hour),
                                                                        makeCycList(C("DayFn"),
                                                                                    FindOrCreateCycFort(dateTime.Day),
                                                                                    makeCycList(C("MonthFn"),
                                                                                                C(dateTime.ToString("MMMM")),
                                                                                                makeCycList(
                                                                                                    C("YearFn"),
                                                                                                    FindOrCreateCycFort(
                                                                                                        dateTime.Year)))))))));

                default:
                    return
                        new CycNart(makeCycList(C("NuSketchSketchTimeFn"),
                                                FindOrCreateCycFort((dateTime - baseTime).Ticks/10000)));
            }
        }

        public CycFort FindOrCreateCycFort(Primitive simObj)
        {
            return FindOrCreateCycFort(WorldObjects.GridMaster.GetSimObject(simObj));
        }

        public CycFort FindOrCreateCycFort(SimObjectEvent evt)
        {
            lock (cycTerms)
            {
                CycFort constant;
                if (cycTerms.TryGetValue(evt, out constant)) return constant;
                //   object[] forts = ToForts(simObj.Parameters);
                constant = createIndividualFn("SimEvent-" + evt.EventType,
                                              evt.ToEventString(),
                                              evt.ToString(),
                                              "SimVocabMt",
                                              "SimObjectEvent");
                bool wasNew;
                CycFort col = createIndividual(evt.GetVerb().Replace(" ", "-"),
                                               "Event subtype of #$SimObjectEvent", "SimVocabMt", "Collection",
                                               out wasNew);
                if (wasNew)
                {
                    assertGenls(col, C("SimObjectEvent"));
                }
                assertIsa(constant, col);
                string[] names = evt.ParameterNames();
                IList<NamedParam> args = evt.Parameters;
                string datePred;
                switch (evt.EventStatus)
                {
                    case SimEventStatus.Start:
                        datePred = (TimeRep == 2 ? "startingPoint" : "startingDate");
                        break;
                    case SimEventStatus.Stop:
                        datePred = (TimeRep == 2 ? "endingPoint" : "endingDate");
                        break;
                    case SimEventStatus.Once:
                    default:
                        datePred = (TimeRep == 2 ? "timePoint" : "dateOfEvent");
                        break;
                }
                assertEventData(constant, datePred, ToFort(evt.Time));

                int i = -1;
                foreach (NamedParam list in args)
                {
                    i++;
                    object o =list.Value;
                    if (o.GetType().IsEnum)
                    {
                        VisitEnumType(o.GetType());
                    }

                    if (list.Key != null)
                    {
                        string k = list.Key.ToString();
                        if (k == "isa")
                        {
                            if (o is String)
                            {
                                assertIsa(constant,C(o.ToString()));
                                continue;
                            }
                        }
                        if (k == "senderOfInfo" || k == "doneBy")
                        {
                            if (o is String)
                            {
                                o = new CycNart(C("SimAvatarFn"), o.ToString());
                            }
                        }
                        assertEventData(constant, list.Key.ToString(), ToFort(o));
                    }
                    else
                    {
                        o = args[i].Value;
                        if (o.GetType().IsEnum)
                        {
                            assertIsa(constant, (CycFort)ToFort(o));
                        }
                        else
                        {
                            assertEventData(constant, names[i], ToFort(o));
                        }
                    }
                }
                return constant;
            }
        }

        private void assertEventData(CycFort constant, string name, object fort)
        {
            CycList list = makeCycList(createEventProperty(name), constant, fort);
            try
            {
                string ast = list.cyclifyWithEscapeChars();

                if (!cycAssert(ast))
                {
                    Debug("Assertion Failed: " + ast);
                }
            }
            catch (java.lang.RuntimeException re)
            {
                re.printStackTrace();
                Debug("" + re);
            }

        }

        private object createEventProperty(string p)
        {
            return createIndividual(p, "sim event property", "SimVocabMt", "BinaryPredicate");
        }


        public CycFort createIndividualFn(string typename, string name, string comment, string simvocabmt, string simobjecttype)
        {
            bool newlyCreated;
            CycFort fn = createIndividual(typename + "Fn", comment,
                                          simvocabmt, "UnaryFunction", out newlyCreated);
            if (newlyCreated)
            {
                assertGaf(C("isa"), fn, C("ReifiableFunction"));
                assertGaf(C("resultIsa"), fn, C(simobjecttype));
            }
            CycFort indv;
            bool b = typename.StartsWith("SimEvent");
            if (b) // right now we dont intern Events
            {
                indv = new CycNart(CycList.list(fn, name));
            }
            else
                lock (cycTerms)
                {
                    {
                        string nv = name + "-" + typename;
                        if (cycTerms.TryGetValue(nv, out indv)) return indv;
                        indv = new CycNart(CycList.list(fn, name));
                        cycTerms[nv] = indv;
                        assertGaf(C("comment"), indv, comment);
                    }
                }
            return indv;
        }

        private bool IndividualExists(CycFort indv)
        {
            return cycAccess.converseInt("(length (term-assertions '" + indv.cyclifyWithEscapeChars() + "))") > 1;
        }


        public object[] ToForts(object[] parameters)
        {
            object[] forts = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                forts[i] = ToFort(parameters[i]);
            }
            return forts;
        }

        public object ToFort(object parameter)
        {
            if (parameter == null) return CYC_NULL;
            Type t = parameter.GetType();
            var conv = GetConverter(t);
            if (conv != null)
            {
                object obj = conv.Invoke(parameter);
                return obj;
            }
            if (t.IsEnum)
            {
                VisitEnumType(t);
                return C(string.Format("{0}-{1}", t.Name, parameter.ToString()));
            }
            if (t.IsValueType)
            {
                try
                {
                    return C(string.Format("{0}-{1}", t.Name, parameter.ToString()));
                }
                catch (Exception e)
                {
                    Debug("" + e);
                }
            }
            Debug("Cant convert " + t);
            return CYC_NULL;
        }


        public CycFort FindOrCreateCycFort(Vector3 b)
        {
            return new CycNart(makeCycList(C("TheList"),
                new java.lang.Float(b.X),
                new java.lang.Float(b.Y),
                new java.lang.Float(b.Z)));
        }

        public object FindOrCreateCycFort(GridClient client)
        {
            return FindOrCreateCycFort(client.Self.AgentID);
        }

        public object FindOrCreateCycFort(cogbot.BotClient client)
        {
            return FindOrCreateCycFort(client.WorldSystem);
        }

        public object FindOrCreateCycFort(WorldObjects client)
        {
            return FindOrCreateCycFort(client.TheSimAvatar);
        }



        public object FindOrCreateCycFort(Single b)
        {
            return new java.lang.Float(b);
        }
        public object FindOrCreateCycFort(double b)
        {
            return new java.lang.Double(b);
        }
        public object FindOrCreateCycFort(int b)
        {
            return new java.lang.Integer(b);
        }
        public object FindOrCreateCycFort(long b)
        {
            return new java.lang.Long(b);
        }

        public CycFort FindOrCreateCycFort(bool b)
        {
            return b ? C("True") : C("False");
        }

        static public readonly Dictionary<Type, Converter<object, object>> converters = new Dictionary<Type, Converter<object, object>>();
        static object PassThruConversion(object inp)
        {
            return inp;
        }
        
        void LoadConverters()
        {
            lock (converters)
            {
                if (converters.Count > 0) return;
                converters[typeof(string)] = PassThruConversion;
                converters[typeof(CycObject)] = PassThruConversion;
                foreach (var fort in typeof(SimCyclifier).GetMethods())
                {
                    if (fort.Name != "FindOrCreateCycFort") continue;
                    ParameterInfo[] ps = fort.GetParameters();
                    if (ps.Length != 1) continue;
                    Type ptype = ps[0].ParameterType;
                    if (converters.ContainsKey(ptype)) continue;
                    converters[ptype] = MethodInfoToConverter(ptype, this, fort);
                    Debug("Created conversion " + ptype.FullName);
                }
                //converters[typeof(IConvertible)] = PassThruConversion;
            }
        }

        static Converter<object, object> MethodInfoToConverter(Type sane, SimCyclifier cyclifier, MethodInfo info)
        {
            Converter<object, object> ret = delegate(object inp)
                                                {
                                                    if (inp == null) return CYC_NULL;
                                                    if (!sane.IsInstanceOfType(inp))
                                                    {
                                                        throw new ArgumentException("" + inp + " is not " + sane);
                                                    }
                                                    try
                                                    {
                                                        return info.Invoke(cyclifier, new[] { inp });
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string errr = "" + e;
                                                        Debug(errr);
                                                        return errr;
                                                    }
                                                };
            return ret;
        }

        static Converter<object, object> MethodInfoToConverterFunSyntax(Type sane, SimCyclifier cyclifier, MethodInfo info)
        {
            return inp =>
                       {
                           if (inp == null) return CYC_NULL;
                           if (!sane.IsInstanceOfType(inp))
                               throw new ArgumentException("" + inp + " is not " + sane);
                           return info.Invoke(cyclifier, new[] { inp });
                       };
        }

        public static int nullCount = 0;
        public static CycObject CYC_NULL
        {
            get { return new CycVariable("NULL-" + (nullCount++)); }
        }

        public Converter<object, object> GetConverter(Type type)
        {
            lock (converters)
            {
                LoadConverters();
                //one day will be Converter<object, CycFort> converter;
                Converter<object, object> converter;
                if (converters.TryGetValue(type, out converter)) return converter;
                foreach (var v in converters)
                {
                    if (type.IsSubclassOf(v.Key))
                    {
                        return converters[type] = v.Value;
                    }
                }
                foreach (var v in converters)
                {
                    if (!v.Key.IsValueType && v.Key.IsAssignableFrom(type))
                    {
                        return converters[type] = v.Value;
                    }
                }
            }
            return null;
        }

        public CycObject FindOrCreateCycFort(Simulator sim)
        {
            return FindOrCreateCycFort(SimRegion.GetRegion(sim));
        }
        public CycFort FindOrCreateCycFort(Vector3 simObjectEvent, Object regionName)
        {
            CycList makeCycList1 = makeCycList(C("PointInRegionFn"),
                                               regionName,
                                               FindOrCreateCycFort(simObjectEvent.X),
                                               FindOrCreateCycFort(simObjectEvent.Y),
                                               FindOrCreateCycFort(simObjectEvent.Z));
            return new CycNart(makeCycList1);
        }

        public static CycList makeCycList(params object[] argsObject)
        {
            CycList list = new CycList(argsObject.Length);
            foreach (object e in argsObject)
            {
                list.add(e);
            }
            return list;
        }

        public CycFort C(string p)
        {
            if (simFort.ContainsKey(p)) return simFort[p];
            try
            {
                return cycAccess.findOrCreate(p);
            } catch(Exception e)
            {
                Exception(e);
                throw e;
                //return CYC_NULL;
            }
        }

        public object FindOrCreateCycFort(UUID region)
        {
            if (region == UUID.Zero) return CYC_NULL;
            object o = WorldObjects.GridMaster.GetObject(region);
            if (!(o is UUID)) return ToFort(o);
            return "" + region;
            //return createIndividualFn("SimRegion", region.RegionName, vocabMt.ToString(), "SimRegion " + region, "GeographicalPlace-3D");
        }

        public CycObject FindOrCreateCycFort(SimRegion region)
        {
            if (region == SimRegion.UNKNOWN) return CYC_NULL;
            return createIndividualFn("SimRegion", region.RegionName, "SimRegion " + region, vocabMt.ToString(), "GeographicalPlace-3D");
        }

        public CycObject FindOrCreateCycFort(SimPathStore region)
        {
            if (region.RegionName == "<0,0>") return CYC_NULL;
            return createIndividualFn("SimRegion", region.RegionName, "SimRegion " + region, vocabMt.ToString(), "GeographicalPlace-3D");
        }

        public CycObject FindOrCreateCycFort(SimHeading b)
        {
            if (SimHeading.UNKNOWN == b) return CYC_NULL;
            if (b.IsRegionAttached())
            {
                SimPathStore r = b.GetPathStore();
                CycObject findOrCreateCycFort = FindOrCreateCycFort(r);
                return FindOrCreateCycFort(b.GetSimPosition(), r.RegionName);
            } else
            {
                object arg1 = ToFort(b.GetRoot());
                Vector3 offset = b.GetOffset();
                //return "PointRelativeToFn";
                return new CycNart(makeCycList(C("PointRelativeToFn"), arg1,
                                               FindOrCreateCycFort(offset.X),
                                               FindOrCreateCycFort(offset.Y),
                                               FindOrCreateCycFort(offset.Z)));
            }
        }

        public CycFort FindOrCreateCycFort(Vector3d simObjectEvent)
        {
            SimRegion r = SimRegion.GetRegion(simObjectEvent);
            CycObject findOrCreateCycFort = FindOrCreateCycFort(r);
            return FindOrCreateCycFort(SimRegion.GlobalToLocal(simObjectEvent), r.RegionName);
        }

        public object FindOrCreateCycFort(uint b)
        {
            return new java.lang.Long(b);
        }

        public object FindOrCreateCycFort(ushort b)
        {
            return new java.lang.Integer(b);
        }

        public object FindOrCreateCycFort(char b)
        {
            return new java.lang.Character(b);
        }

        public CycFort FindOrCreateCycFort(BotSocialAction botSocialAction)
        {
            throw new NotImplementedException();
        }

        public CycFort FindOrCreateCycFort(BotObjectAction botObjectAction)
        {
            throw new NotImplementedException();
        }

        public CycFort FindOrCreateCycFort(Primitive.TextureEntry te)
        {
            return createIndividualFn("SimTextEntry", te);
        }
        public CycFort FindOrCreateCycFort(Avatar.AvatarProperties te)
        {
            return createIndividualFn("SimAvatarProperties", te);
        }

        private CycFort createIndividualFn(string p, object te)
        {
            lock (cycTerms)
            {
                CycFort constant;
                if (cycTerms.TryGetValue(te, out constant)) return constant;
                //   object[] forts = ToForts(simObj.Parameters);
                return cycTerms[te] = createIndividualFn(p, te.ToString(), te.ToString(), "SimVocabMt", p);
            }
        }

        public CycFort FindOrCreateCycFort(SimTypeUsage simTypeUsage)
        {
            throw new NotImplementedException();
        }

        public CycFort FindOrCreateCycFort(SimObjectUsage simObjectUsage)
        {
            throw new NotImplementedException();
        }

        public CycFort FindOrCreateCycFort(MoveToLocation moveToLocation)
        {
            throw new NotImplementedException();
        }

        public void World_OnSimObject(SimObject obj)
        {
          //  FindOrCreateCycFort(obj);
        }
    }
}
