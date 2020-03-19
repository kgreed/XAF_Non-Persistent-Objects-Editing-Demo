using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
