using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;

namespace NonPersistentObjectsDemo.Module {

    class TransientNonPersistentObjectAdapter<TObject, TKey> {
        private NonPersistentObjectSpace objectSpace;
        private NonPersistentObjectFactoryBase factory;
        private Dictionary<TKey, TObject> objectMap;
        public TransientNonPersistentObjectAdapter(NonPersistentObjectSpace objectSpace, NonPersistentObjectFactoryBase factory) {
            this.objectSpace = objectSpace;
            this.factory = factory;
            objectMap = new Dictionary<TKey, TObject>();
            objectSpace.ObjectsGetting += ObjectSpace_ObjectsGetting;
            objectSpace.ObjectGetting += ObjectSpace_ObjectGetting;
            objectSpace.ObjectByKeyGetting += ObjectSpace_ObjectByKeyGetting;
            objectSpace.Reloaded += ObjectSpace_Reloaded;
            objectSpace.CustomCommitChanges += ObjectSpace_CustomCommitChanges;
        }
        private void ObjectSpace_CustomCommitChanges(object sender, HandledEventArgs e) {
            var toSave = objectSpace.GetObjectsToSave(false).OfType<TObject>();
            var toInsert = new List<TObject>();
            var toUpdate = new List<TObject>();
            foreach(var obj in toSave) {
                if(objectSpace.IsNewObject(obj)) {// or check objectMap?
                    toInsert.Add(obj);
                }
                else {
                    toUpdate.Add(obj);
                }
            }
            var toDelete = objectSpace.GetObjectsToDelete(false).OfType<TObject>().ToList();
            factory.SaveObjects(toInsert, toUpdate, toDelete);
            //e.Handled = false;// !!!
        }
        private void ObjectSpace_Reloaded(object sender, EventArgs e) {
            objectMap.Clear();
        }
        private void ObjectSpace_ObjectByKeyGetting(object sender, ObjectByKeyGettingEventArgs e) {
            if(e.ObjectType == typeof(TObject) && e.Key != null) {
                var key = (TKey)e.Key;
                TObject obj;
                if(!objectMap.TryGetValue(key, out obj)) {
                    obj = (TObject)factory.GetObjectByKey(typeof(TObject), key);
                    objectMap.Add(key, obj);
                }
                e.Object = obj;
            }
        }
        private void ObjectSpace_ObjectGetting(object sender, ObjectGettingEventArgs e) {
            if(e.SourceObject is TObject) {
                if(!objectMap.ContainsValue((TObject)e.SourceObject)) {
                    var key = objectSpace.GetKeyValue(e.SourceObject);
                    e.TargetObject = objectSpace.GetObjectByKey<TObject>(key);
                }
            }
        }
        private void ObjectSpace_ObjectsGetting(object sender, ObjectsGettingEventArgs e) {
            if(e.ObjectType == typeof(TObject)) {
                var collection = new DynamicCollection(objectSpace, e.ObjectType, e.Criteria, e.Sorting, e.InTransaction);
                collection.ObjectsGetting += DynamicCollection_ObjectsGetting;
                e.Objects = collection;
            }
        }
        private IList GetList(Type objectType, CriteriaOperator criteria, IList<DevExpress.Xpo.SortProperty> sorting) {
            var query = factory.GetObjectKeys(objectType, criteria, sorting);
            var list = new List<TObject>();
            foreach(var key in query) {
                TObject obj = objectSpace.GetObjectByKey<TObject>(key);
                list.Add(obj);
            }
            return list;
        }
        private void DynamicCollection_ObjectsGetting(object sender, DynamicObjectsGettingEventArgs e) {
            e.Objects = GetList(e.ObjectType, e.Criteria, e.Sorting);
            e.ShapeRawData = true;
        }
    }
}
