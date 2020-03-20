using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;

namespace NonPersistentObjectsDemo.Module {

    public class CurrentUserServiceProvider<T> {
        private static Func<T> factory;
        public static void AddService(Func<T> factory) {
            CurrentUserServiceProvider<T>.factory = factory;
        }
        public static T GetService() {
            var vm = ValueManager.GetValueManager<T>(typeof(CurrentUserServiceProvider<T>).FullName);
            T result;
            if(vm.CanManageValue) {
                result = vm.Value;
                if(Equals(result, default(T))) {
                    result = factory.Invoke();
                    vm.Value = result;
                }
            }
            else {
                result = factory.Invoke();
            }
            return result;
        }
    }

    public class GlobalServiceProvider<T> {
        private static Func<T> factory;
        private static T instance;
        public static void AddService(Func<T> factory) {
            GlobalServiceProvider<T>.factory = factory;
        }
        public static T GetService() {
            typeof(T).TypeInitializer.Invoke(null, null);
            if(Equals(instance, default(T))) {
                instance = factory.Invoke();
            }
            return instance;
        }
    }

    public class Storage {
        static Storage() {
            CurrentUserServiceProvider<Storage>.AddService(() => {
                var storage = new Storage();
                storage.LoadDemoData();
                return storage;
            });
        }

        private DataSet dataSet;
        public Storage() {
            dataSet = new DataSet();
            var dt = dataSet.Tables.Add("TableA");
            var colID = dt.Columns.Add("ID", typeof(Guid));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Group", typeof(int));
            dt.PrimaryKey = new DataColumn[] { colID };
        }
        public DataRow GetDataByKey(string tableName, object key) {
            return dataSet.Tables[tableName].Rows.Find(key);
        }
        public IEnumerable GetAllKeys(string tableName) {
            var table = dataSet.Tables[tableName];
            return table.Select().Select(r => r[table.PrimaryKey[0]]);
        }
        public void LoadDemoData() {
            var dt = dataSet.Tables["TableA"];
            dt.LoadDataRow(new object[] { Guid.NewGuid(), "X", 1 }, LoadOption.OverwriteChanges);
            dt.LoadDataRow(new object[] { Guid.NewGuid(), "Y", 2 }, LoadOption.OverwriteChanges);
            dt.LoadDataRow(new object[] { Guid.NewGuid(), "Z", 0 }, LoadOption.OverwriteChanges);
        }
    }
}

namespace Experimental {

    interface ISelectionRequest { }
    interface ISelectionResponse { }
    interface IModificationRequest { }
    interface IModificationResponse { }

    interface Transaction {

    }

    interface Storage {
        ISelectionResponse Select(ISelectionRequest request);
        IModificationResponse Modify(IModificationRequest request);
    }

    /// <summary>
    /// This is only a set of methods.
    /// The object doesn't keep any data. Not even a reference to the storage
    /// </summary>
    interface ObjectStorageMediator {
        // create a selaction request from criteria
        ISelectionRequest PrepareLoading(CriteriaOperator criteria);

        // process the response, update objects found in OS or create and add new ones, return the top collection
        ICollection PostProcessLoading(object os, ISelectionResponse response);

        // process changed objects and create a request for the storage
        IModificationRequest PrepareChanges(ICollection objects);

        // process modification results (update inserted object keys, versions, delete flags)
        void PostProcessChanges(object os, ICollection objects, IModificationResponse response);
    }

    interface ObjectFactory {
        //ICollection Load();
        object CreateNewObject(Type objectType);
        //object LoadObject(Type objectType, object key);
        //void ReloadObject(object obj);

    }

    interface Adapter {
        ICollection GetObjects(CriteriaOperator filter, DevExpress.Xpo.SortProperty[] sorting, int skip, int take);
        void SaveObjects(ICollection objects);
    }
}
