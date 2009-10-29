using System;
using System.Collections;
using System.Collections.Generic;
using cogbot.ScriptEngines;
using cogbot.TheOpenSims;
using OpenMetaverse;
using PathSystem3D.Navigation;

namespace cogbot.Listeners
{
    public class DefaultWorldGroupProvider : ICollectionProvider
    {
        readonly WorldObjects world;
        public DefaultWorldGroupProvider(WorldObjects objects)
        {
            world = objects;
            AddObjectGroup("selected", () =>
            {
                SimActor avatar = this.avatar;
                if (avatar == null) return null; 
                return avatar.GetSelectedObjects();
            });
            AddObjectGroup("avatars", () => WorldObjects.SimAvatars.CopyOf());
            AddObjectGroup("master", () =>
                                         {
                                             var v = new List<object>();
                                             if (objects.client.MasterKey != UUID.Zero)
                                             {
                                                 v.Add(objects.CreateSimAvatar(objects.client.MasterKey, objects, null));

                                             }
                                             else
                                             {
                                                 v.Add(objects.client.MasterName);
                                             }
                                             return v;
                                         });
            AddObjectGroup("self", () =>
                                       {
                                           SimActor avatar = this.avatar;
                                           if (avatar == null) return null; 
                                           var v = new List<SimObject> { avatar }; return v;
                                       });
            AddObjectGroup("all", () => WorldObjects.SimObjects.CopyOf());
            AddObjectGroup("known", () =>
                                        {
                                            SimActor avatar = this.avatar;
                                            if (avatar == null) return null; 
                                            return avatar.GetKnownObjects();
                                        });
            AddObjectGroup("target", () =>
                                         {
                                             SimActor avatar = this.avatar;
                                             if (avatar == null) return null;
                                             var v = new List<SimPosition>();
                                             var a = avatar.CurrentAction;
                                             if (a != null && a.Target != null)
                                             {
                                                 v.Add(a.Target);
                                                 return v;
                                             }
                                             SimPosition p = avatar.ApproachPosition;
                                             if (p != null)
                                             {
                                                 v.Add(p);
                                                 return v;
                                             }
                                             return null;
                                         });
        }

        protected SimActor avatar
        {
            get { return world.TheSimAvatar; }
        }

        public ICollection GetGroup(string arg0Lower)
        {
            Func<IList> func;
            if (ObjectGroups.TryGetValue(arg0Lower, out func))
            {
                if (func == null) return null;
                return func();
            }
            return null;
        }

        readonly Dictionary<string, Func<IList>> ObjectGroups = new Dictionary<string, Func<IList>>();
        public void AddObjectGroup(string selecteditems, Func<IList> func)
        {
            lock (ObjectGroups)
            {
                ObjectGroups[selecteditems.ToLower()] = func;
            }
        }
    }
}