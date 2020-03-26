using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;

namespace NonPersistentObjectsDemo.Module.BusinessObjects {

    [NavigationItem("PostOffice")]
    [DefaultProperty("Subject")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Message : NonPersistentObjectBase {
        private int id;
        [Browsable(false)]
        [DevExpress.ExpressApp.Data.Key]
        public int ID {
            get { return id; }
        }
        public void SetKey(int id) {
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
        private bool isLoading = false;
        private ObjectMap objectMap;
        public PostOfficeFactory(ObjectMap objectMap) {
            this.objectMap = objectMap;
        }
        public override object GetObjectByKey(Type objectType, object key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if(Storage.Mappings.TryGetValue(objectType, out var mapping)) {
                return WrapLoading(() => {
                    var loader = new DataStoreObjectLoader(Storage.Mappings, Storage.DataStore, objectMap);
                    return loader.LoadObjectByKey(objectType, key);
                });
            }
            throw new NotImplementedException();
        }
        public override IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<SortProperty> sorting) {
            if(Storage.Mappings.TryGetValue(objectType, out var mapping)) {
                var objects = WrapLoading(() => {
                    var loader = new DataStoreObjectLoader(Storage.Mappings, Storage.DataStore, objectMap);
                    return loader.LoadObjects(objectType, criteria);
                });
                return objects.Select(o => mapping.GetKey(o)).ToArray();
            }
            throw new NotImplementedException();
        }
        private T WrapLoading<T>(Func<T> doer) {
            if(isLoading) {
                throw new InvalidOperationException();
            }
            isLoading = true;
            try {
                return doer.Invoke();
            }
            finally {
                isLoading = false;
            }
        }
        public override void SaveObjects(ICollection toInsert, ICollection toUpdate, ICollection toDelete) {
            var saver = new DataStoreObjectSaver(Storage.Mappings, Storage.DataStore);
            saver.SaveObjects(toInsert, toUpdate, toDelete);
        }
    }


    public class PostOfficeClient {
        static PostOfficeClient() {
            GlobalServiceProvider<PostOfficeClient>.AddService(() => new PostOfficeClient());
        }
        public Dictionary<Type, DataStoreMapping> Mappings { get; private set; }
        public IDataStore DataStore { get; private set; }
        public PostOfficeClient() {
            this.DataStore = new InMemoryDataStore();
            this.Mappings = new Dictionary<Type, DataStoreMapping>();
            var mAccount = new DataStoreMapping();
            mAccount.Table = new DBTable("Accounts");
            mAccount.Table.AddColumn(new DBColumn("UserName", true, null, 255, DBColumnType.String));
            mAccount.Table.AddColumn(new DBColumn("PublicName", false, null, 1024, DBColumnType.String));
            mAccount.Create = () => new Account();
            mAccount.Load = (obj, values, omap) => {
                ((Account)obj).SetKey((string)values[0]);
                ((Account)obj).PublicName = (string)values[1];
            };
            mAccount.Save = (obj, values) => {
                values[0] = ((Account)obj).UserName;
                values[1] = ((Account)obj).PublicName;
            };
            mAccount.GetKey = (obj) => ((Account)obj).UserName;
            mAccount.RefColumns = Enumerable.Empty<DataStoreMapping.Column>();
            Mappings.Add(typeof(Account), mAccount);
            var mMessage = new DataStoreMapping();
            mMessage.Table = new DBTable("Messages");
            var mMessageKey = new DBColumn("ID", true, null, 0, DBColumnType.Int32);
            mMessageKey.IsIdentity = true;
            mMessage.Table.AddColumn(mMessageKey);
            mMessage.Table.AddColumn(new DBColumn("Subject", false, null, 1024, DBColumnType.String));
            mMessage.Table.AddColumn(new DBColumn("Body", false, null, -1, DBColumnType.String));
            mMessage.Table.AddColumn(new DBColumn("Sender", false, null, 255, DBColumnType.String));
            mMessage.Table.AddColumn(new DBColumn("Recepient", false, null, 255, DBColumnType.String));
            mMessage.Table.PrimaryKey = new DBPrimaryKey(new object[] { mMessageKey });
            mMessage.Create = () => new Message();
            mMessage.SetKey = (obj, key) => {
                ((Message)obj).SetKey((int)key);
            };
            mMessage.GetKey = (obj) => ((Message)obj).ID;
            mMessage.Load = (obj, values, omap) => {
                var o = (Message)obj;
                o.SetKey((int)values[0]);
                o.Subject = (string)values[1];
                o.Body = (string)values[2];
                o.Sender = omap.Get<Account>(values[3]);
                o.Recepient = omap.Get<Account>(values[4]);
            };
            mMessage.Save = (obj, values) => {
                var o = (Message)obj;
                values[0] = o.ID;
                values[1] = o.Subject;
                values[2] = o.Body;
                values[3] = o.Sender?.UserName;
                values[4] = o.Recepient?.UserName;
            };
            mMessage.RefColumns = new DataStoreMapping.Column[] {
                new DataStoreMapping.Column(){ Index = 3, Type = typeof(Account) },
                new DataStoreMapping.Column(){ Index = 4, Type = typeof(Account) }
            };
            Mappings.Add(typeof(Message), mMessage);
            DataStore.UpdateSchema(false, mAccount.Table, mMessage.Table);
            CreateDemoData();
        }
        void CreateDemoData() {
            var inMemoryDataStore = (InMemoryDataStore)DataStore;
            var ds = new DataSet();
            using(var ms = new System.IO.MemoryStream()) {
                using(var writer = System.Xml.XmlWriter.Create(ms)) {
                    inMemoryDataStore.WriteXml(writer);
                    writer.Flush();
                }
                ms.Flush();
                ms.Position = 0;
                ds.ReadXml(ms);
            }
            var rnd = new Random();
            var idsAccount = new List<string>();
            var dtAccounts = ds.Tables["Accounts"];
            for(int i = 0; i < 200; i++) {
                var id = MakeTosh(rnd, 20);
                idsAccount.Add(id);
                dtAccounts.Rows.Add(id, "User-" + id);
            }
            var dtMessages = ds.Tables["Messages"];
            for(int i = 0; i < 5000; i++) {
                var id1 = rnd.Next(idsAccount.Count);
                var id2 = rnd.Next(idsAccount.Count - 1);
                dtMessages.Rows.Add(null, MakeBlah(rnd, rnd.Next(7)), MakeBlah(rnd, 5 + rnd.Next(100)),
                    idsAccount[id1], idsAccount[(id1 + id2 + 1) % idsAccount.Count]);
            }
            ds.AcceptChanges();
            using(var ms = new System.IO.MemoryStream()) {
                ds.WriteXml(ms, XmlWriteMode.WriteSchema);
                ms.Flush();
                ms.Position = 0;
                using(var reader = System.Xml.XmlReader.Create(ms)) {
                    inMemoryDataStore.ReadXml(reader);
                }
            }
        }
        private string MakeTosh(Random rnd, int length) {
            var chars = new char[length];
            for(int i = 0; i < length; i++) {
                chars[i] = (char)('a' + rnd.Next(26));
            }
            return new String(chars);
        }
        private string MakeBlah(Random rnd, int length) {
            var sb = new StringBuilder();
            for(var i = 0; i <= length; i++) {
                if(sb.Length > 0) {
                    sb.Append(" ");
                }
                var w = MakeTosh(rnd, 1 + rnd.Next(13));
                sb.Append(w);
            }
            return sb.ToString();
        }
    }

}
