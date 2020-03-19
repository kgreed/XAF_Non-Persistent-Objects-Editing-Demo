using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;

namespace NonPersistentObjectsDemo.Module {

    public abstract class NonPersistentObjectFactoryBase {
        public abstract object GetObjectByKey(Type objectType, object key);
        public abstract IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<DevExpress.Xpo.SortProperty> sorting);
    }

    class NonPersistentObjectFactory : NonPersistentObjectFactoryBase {
        Storage storage;
        public NonPersistentObjectFactory(Storage storage) {
            this.storage = storage;
        }
        public override object GetObjectByKey(Type objectType, object key) {
            //if(typeof(NPObjectA) == objectType) {
            //    if(key == null) {
            //        throw new ArgumentNullException(nameof(key));
            //    }
            //    var data = storage.GetDataByKey("TableA", key);
            //    if(data != null) {
            //        var obj = new NPObjectA();
            //        obj.SetKey((Guid)key);
            //        obj.Name = Convert.ToString(data["Name"]);
            //        obj.Group = Convert.ToInt32(data["Group"]);
            //        return obj;
            //    }
            //    else {
            //        return null;
            //    }
            //}
            throw new NotImplementedException();
        }
        public override IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<DevExpress.Xpo.SortProperty> sorting) {
            //if(typeof(NPObjectA) == objectType) {
            //    return storage.GetAllKeys("TableA");
            //}
            throw new NotImplementedException();
        }
    }
}
