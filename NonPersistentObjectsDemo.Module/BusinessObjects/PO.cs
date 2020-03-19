using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
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
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Account : NonPersistentObjectBase {
        private string userName;
        [Browsable(false)]
        [DevExpress.ExpressApp.Data.Key]
        public string UserName {
            get { return userName; }
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
        private static Lazy<PostOfficeClient> storage = new Lazy<PostOfficeClient>(() => new PostOfficeClient());
        public override object GetObjectByKey(Type objectType, object key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if(typeof(Account) == objectType) {
                var data = storage.Value.GetAccounts(new BinaryOperator("UserName", (string)key).ToString());
                if(data != null && data.Count == 1) {
                    return CreateAccount(data[0]);
                }
                else {
                    return null;
                }
            }
            if(typeof(Message) == objectType) {
                var data = storage.Value.GetMessages(new BinaryOperator("ID", (string)key).ToString());
                if(data != null && data.Count == 1) {
                    return CreateMessage(data[0]);
                }
                else {
                    return null;
                }
            }
            throw new NotImplementedException();
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
            obj.Sender = (Account)GetObjectByKey(typeof(Account), record["Sender"]);
            obj.Recepient = (Account)GetObjectByKey(typeof(Account), record["Recepient"]);
            return obj;
        }
        public override IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<SortProperty> sorting) {
            if(typeof(Account) == objectType) {
                var data = storage.Value.GetAccounts(CriteriaOperator.ToString(criteria));
                return data.Select(d => d["UserName"]).ToArray();
            }
            if(typeof(Message) == objectType) {
                var data = storage.Value.GetMessages(CriteriaOperator.ToString(criteria));
                return data.Select(d => d["ID"]).ToArray();
            }
            throw new NotImplementedException();
        }
        public override void SaveObjects(ICollection toInsert, ICollection toUpdate, ICollection toDelete) {
            throw new NotImplementedException();
        }
    }

    public class PostOfficeClient {
        private DataSet dataSet;
        public IList<DataRow> GetAccounts(string filter) {
            var f = CriteriaToWhereClauseHelper.GetDataSetWhere(CriteriaOperator.Parse(filter));
            return dataSet.Tables["Accounts"].Select(f);
        }
        public IList<DataRow> GetMessages(string filter) {
            var f = CriteriaToWhereClauseHelper.GetDataSetWhere(CriteriaOperator.Parse(filter));
            return dataSet.Tables["Messages"].Select(f);
        }
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
    }
}
