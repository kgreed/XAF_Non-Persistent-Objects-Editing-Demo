using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Persistent.Base;

namespace NonPersistentObjectsDemo.Module {

    public class Storage {
        private static Storage CreateInstance() {
            var storage = new Storage();
            storage.LoadDemoData();
            return storage;
        }
        public static Storage GetInstance() {
            var vm = ValueManager.GetValueManager<Storage>("NP");
            Storage storage = null;
            if(vm.CanManageValue) {
                storage = vm.Value;
                if(storage == null) {
                    storage = CreateInstance();
                    vm.Value = storage;
                }
            }
            else {
                storage = CreateInstance();
            }
            return storage;
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
