using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace NonPersistentObjectsDemo.Module.BusinessObjects {

    [NavigationItem("PostOffice")]
    [DefaultProperty("Subject")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Message : NonPersistentObjectBase {
        private string id;
        [Browsable(false)]
        [DevExpress.ExpressApp.Data.Key]
        public string ID {
            get { return id; }
        }
        public void SetKey(string id) {
            this.id = id;
        }
        private Account _Sender;
        public Account Sender {
            get { return _Sender; }
            set { SetPropertyValue(nameof(Sender), ref _Sender, value); }
        }
        private Account _Recepient;
        public Account Recepient {
            get { return _Recepient; }
            set { SetPropertyValue(nameof(Recepient), ref _Recepient, value); }
        }
        private string _Subject;
        public string Subject {
            get { return _Subject; }
            set { SetPropertyValue<string>(nameof(Subject), ref _Subject, value); }
        }
        private string _Body;
        [FieldSize(-1)]
        public string Body {
            get { return _Body; }
            set { SetPropertyValue<string>(nameof(Body), ref _Body, value); }
        }
    }

    [NavigationItem("PostOffice")]
    [DefaultProperty("PublicName")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Account : NonPersistentObjectBase {
        private string userName;
        //[Browsable(false)]
        [DevExpress.ExpressApp.ConditionalAppearance.Appearance("", Enabled = false, Criteria = "Not IsNewObject(This)")]
        [DevExpress.ExpressApp.Data.Key]
        public string UserName {
            get { return userName; }
            set { userName = value; }
        }
        public void SetKey(string userName) {
            this.userName = userName;
        }
        private string publicName;
        public string PublicName {
            get { return publicName; }
            set { SetPropertyValue(nameof(PublicName), ref publicName, value); }
        }
    }

    public class PostOfficeFactory : NonPersistentObjectFactoryBase {
        private PostOfficeClient Storage => GlobalServiceProvider<PostOfficeClient>.GetService();
        private IObjectSpace objectSpace;
        public PostOfficeFactory(IObjectSpace objectSpace) {
            this.objectSpace = objectSpace;
        }
        public override object GetObjectByKey(Type objectType, object key) { // TODO: rename to LoadObject()
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if(typeof(Account) == objectType) {
                var data = GetAccounts(new BinaryOperator("UserName", (string)key));
                if(data != null && data.Count == 1) {
                    return CreateAccount(data[0]);
                }
                else {
                    return null;
                }
            }
            else if(typeof(Message) == objectType) {
                var data = GetMessages(new BinaryOperator("ID", (string)key));
                if(data != null && data.Count == 1) {
                    return CreateMessage(data[0]);
                }
                else {
                    return null;
                }
            }
            throw new NotImplementedException();
        }
        public override IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<SortProperty> sorting) {
            if(typeof(Account) == objectType) {
                var data = GetAccounts(criteria);
                return data.Select(d => d["UserName"]).ToArray();
            }
            else if(typeof(Message) == objectType) {
                var data = GetMessages(criteria);
                return data.Select(d => d["ID"]).ToArray();
            }
            throw new NotImplementedException();
        }
        private IList<DataRow> GetMessages(CriteriaOperator criteria) {
            return Storage.GetRows("Messages", CriteriaOperator.ToString(criteria));
        }
        private IList<DataRow> GetAccounts(CriteriaOperator criteria) {
            return Storage.GetRows("Accounts", CriteriaOperator.ToString(criteria));
        }
        private Account CreateAccount(DataRow record) {
            var obj = new Account();
            obj.SetKey((string)record["UserName"]);
            obj.PublicName = (string)record["PublicName"];
            return obj;
        }
        private Message CreateMessage(DataRow record) {
            var obj = new Message();
            obj.SetKey((string)record["ID"]);
            obj.Subject = (string)record["Subject"];
            obj.Body = (string)record["Body"];
            obj.Sender = objectSpace.GetObjectByKey<Account>(record["Sender"]);
            obj.Recepient = objectSpace.GetObjectByKey<Account>(record["Recepient"]);
            return obj;
        }
        public override void SaveObjects(ICollection toInsert, ICollection toUpdate, ICollection toDelete) {
            foreach(var obj in toDelete) {
                DeleteObject(obj);
            }
            foreach(var obj in toInsert) {
                InsertObject(obj);
            }
            foreach(var obj in toUpdate) {
                UpdateObject(obj);
            }
            Storage.SaveChanges();
        }
        private void GuardNotEmpty(DataRow data) {
            if(data == null) {
                throw new DataException("Row not found in the storage");
            }
        }
        private void DeleteObject(object obj) {
            switch(obj) {
                case Account account: {
                        var data = Storage.FindRow("Accounts", ((Account)obj).UserName);
                        GuardNotEmpty(data);
                        data.Delete();
                    }
                    break;
                case Message message: {
                        var data = Storage.FindRow("Messages", ((Message)obj).ID);
                        GuardNotEmpty(data);
                        data.Delete();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void InsertObject(object obj) {
            switch(obj) {
                case Account account: {
                        var data = Storage.NewRow("Accounts");
                        SaveProperties(data, obj);
                        Storage.Accept(data);
                    }
                    break;
                case Message message: {
                        var data = Storage.NewRow("Messages");
                        SaveProperties(data, obj);
                        Storage.Accept(data);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void UpdateObject(object obj) {
            switch(obj) {
                case Account account: {
                        var data = Storage.FindRow("Accounts", account.UserName);
                        GuardNotEmpty(data);
                        SaveProperties(data, obj);
                        Storage.Accept(data);
                    }
                    break;
                case Message message: {
                        var data = Storage.FindRow("Messages", message.ID);
                        GuardNotEmpty(data);
                        SaveProperties(data, obj);
                        Storage.Accept(data);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private void SetReference(DataRow data, string column, object obj) {
            object key = obj == null ? null : objectSpace.GetKeyValue(obj);
            data[column] = key;
        }
        private void SaveProperties(DataRow data, object obj) {
            switch(obj) {
                case Account account:
                    data["UserName"] = account.UserName;
                    data["PublicName"] = account.PublicName;
                    break;
                case Message message:
                    data["ID"] = message.ID;
                    data["Subject"] = message.Subject;
                    data["Body"] = message.Body;
                    SetReference(data, "Sender", message.Sender);
                    SetReference(data, "Recepient", message.Recepient);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class PostOfficeClient {
        static PostOfficeClient() {
            GlobalServiceProvider<PostOfficeClient>.AddService(() => new PostOfficeClient());
        }
        private DataSet dataSet;
        public PostOfficeClient() {
            dataSet = new DataSet();
            {
                var dt = dataSet.Tables.Add("Accounts");
                var colID = dt.Columns.Add("UserName", typeof(string));
                dt.Columns.Add("PublicName", typeof(string));
                dt.PrimaryKey = new DataColumn[] { colID };
            }
            {
                var dt = dataSet.Tables.Add("Messages");
                var colID = dt.Columns.Add("ID", typeof(string));
                dt.Columns.Add("Subject", typeof(string));
                dt.Columns.Add("Body", typeof(string));
                dt.Columns.Add("Sender", typeof(string));
                dt.Columns.Add("Recepient", typeof(string));
                dt.PrimaryKey = new DataColumn[] { colID };
            }
            LoadDemoData();
        }
        public DataRow GetDataByKey(string tableName, object key) {
            return dataSet.Tables[tableName].Rows.Find(key);
        }
        public IEnumerable GetAllKeys(string tableName) {
            var table = dataSet.Tables[tableName];
            return table.Select().Select(r => r[table.PrimaryKey[0]]);
        }
        public void LoadDemoData() {
            var dt = dataSet.Tables["Accounts"];
            dt.LoadDataRow(new object[] { "Jack1234", "Jack The Nipper" }, LoadOption.OverwriteChanges);
            dt.LoadDataRow(new object[] { "Paul555", "Awesome Paul" }, LoadOption.OverwriteChanges);
            dt.LoadDataRow(new object[] { "HillBilly", "Billy Hill" }, LoadOption.OverwriteChanges);
            var dt2 = dataSet.Tables["Messages"];
            dt2.LoadDataRow(new object[] { "0123456789abcdef", "Feedback", "You're welcome!", "Paul555", "HillBilly" }, LoadOption.OverwriteChanges);
        }

        internal void SaveChanges() {
            dataSet.AcceptChanges();
        }
        internal DataRow FindRow(string tableName, object key) {
            return dataSet.Tables[tableName].Rows.Find(key);
        }
        internal IList<DataRow> GetRows(string tableName, string filter) {
            var f = CriteriaToWhereClauseHelper.GetDataSetWhere(CriteriaOperator.Parse(filter));
            return dataSet.Tables[tableName].Select(f);
        }
        internal DataRow NewRow(string tableName) {
            var table = dataSet.Tables[tableName];
            var row = table.NewRow();
            return row;
        }
        internal void Accept(DataRow row) {
            if(row.Table.Rows.IndexOf(row) < 0) {
                row.Table.Rows.Add(row);
            }
            row.AcceptChanges();
        }
    }
}
