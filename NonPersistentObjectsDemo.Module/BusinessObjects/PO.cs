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
        private IObjectSpace objectSpace;
        private bool isLoading = false;
        private ObjectMap objectMap;
        public PostOfficeFactory(IObjectSpace objectSpace, ObjectMap objectMap) {
            this.objectSpace = objectSpace;
            this.objectMap = objectMap;
        }
        public override object GetObjectByKey(Type objectType, object key) {
            if(key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if(Storage.Mappings.TryGetValue(objectType, out var mapping)) {
                if(isLoading) {
                }
                var objects = LoadObjects(mapping, objectType, BuildByKeyCriteria(objectType, key));
                if(objects.Count == 1) {
                    return objects[0];
                }
                if(objects.Count == 0) {
                    return null;
                }
                throw new DataException();
            }
            throw new NotImplementedException();
        }
        public override IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<SortProperty> sorting) {
            if(Storage.Mappings.TryGetValue(objectType, out var mapping)) {
                var objects = LoadObjects(mapping, objectType, criteria);
                return objects.Select(o => objectSpace.GetKeyValue(o)).ToArray();
            }
            throw new NotImplementedException();
        }
        private CriteriaOperator BuildByKeyCriteria(Type objectType, object key) {
            return new BinaryOperator(objectSpace.GetKeyPropertyName(objectType), key);
        }
        private SelectStatement PhaseOne(DataStoreMapping mapping, Type objectType, CriteriaOperator criteria) {
            var statement = new SelectStatement(mapping.Table, "T");
            statement.Condition = SimpleDataStoreCriteriaVisitor.Process(criteria, mapping.Table, "T");
            foreach(var column in mapping.Table.Columns) {
                statement.Operands.Add(new QueryOperand(column, "T"));
            }
            return statement;
        }
        struct PreResult {
            public DataStoreMapping Mapping;
            public Type ObjectType;
            public SelectStatement Statement;
        }
        private List<object> PhaseTwo(DataStoreMapping mapping, Type objectType, SelectStatementResult result, List<PreResult> toLoad) {
            List<object> objects = new List<object>();
            int keyColumnIndex = mapping.Table.Columns.IndexOf(mapping.Table.GetColumn(objectSpace.GetKeyPropertyName(objectType)));
            List<RefColumn> refColumns = FindReferenceColumns(objectType, mapping);
            foreach(var row in result.Rows) {
                var key = row.Values[keyColumnIndex];
                var obj = objectMap.Get(objectType, key);
                if(obj == null) {
                    obj = mapping.Create();
                    objectMap.Add(objectType, key, obj);
                }
                objects.Add(obj);
                foreach(var member in refColumns) {
                    var criteria = BuildByKeyCriteria(member.ObjectType, row.Values[member.ColumnIndex]);
                    var memberMapping = Storage.Mappings[member.ObjectType];
                    toLoad.Add(new PreResult() {
                        Mapping = memberMapping,
                        ObjectType = member.ObjectType,
                        Statement = PhaseOne(memberMapping, member.ObjectType, criteria)
                    });
                }
            }
            return objects;
        }
        private List<RefColumn> FindReferenceColumns(Type objectType, DataStoreMapping mapping) {
            return mapping.RefColumns.Select(s => new RefColumn() { ObjectType = s.Type, ColumnIndex = s.Index }).ToList();
        }
        struct RefColumn {
            public Type ObjectType;
            public int ColumnIndex;
        }
        private void PhaseThree(DataStoreMapping mapping, List<object> objects, SelectStatementResult result) {
            for(int i = 0; i < objects.Count; i++) {
                mapping.Load(objects[i], result.Rows[i].Values, objectSpace);
            }
        }
        struct PostResult {
            public DataStoreMapping Mapping;
            public List<object> Objects;
            public SelectStatementResult Result;
        }
        private IList<object> LoadObjects(DataStoreMapping mapping0, Type objectType0, CriteriaOperator criteria) {
            isLoading = true;
            try {
                List<object> objects0 = null;
                var preResults = new List<PreResult>();
                var postResults = new List<PostResult>();
                var statement0 = PhaseOne(mapping0, objectType0, criteria);
                preResults.Add(new PreResult() { Mapping = mapping0, ObjectType = objectType0, Statement = statement0 });
                while(preResults.Count > 0) {
                    var statements = preResults.Select(p => p.Statement).ToArray();
                    var selectedData = Storage.DataStore.SelectData(statements);
                    var toLoad = new List<PreResult>();
                    for(int i = 0; i < selectedData.ResultSet.Length; i++) {
                        var mapping = preResults[i].Mapping;
                        var result = selectedData.ResultSet[i];
                        var objects = PhaseTwo(mapping, preResults[i].ObjectType, result, toLoad);
                        if(objects0 == null) {
                            objects0 = objects;
                        }
                        postResults.Add(new PostResult() { Mapping = mapping, Objects = objects, Result = result });
                    }
                    preResults = toLoad;
                }
                foreach(var postResult in postResults) {
                    PhaseThree(postResult.Mapping, postResult.Objects, postResult.Result);
                }
                return objects0;
            }
            finally {
                isLoading = false;
            }
        }
        public override void SaveObjects(ICollection toInsert, ICollection toUpdate, ICollection toDelete) {
            var statements = new List<ModificationStatement>();
            var identityAwaiters = new List<Action<object>>();
            foreach(var obj in toDelete) {
                DeleteObject(obj, statements);
            }
            foreach(var obj in toInsert) {
                InsertObject(obj, statements, identityAwaiters);
            }
            foreach(var obj in toUpdate) {
                UpdateObject(obj, statements);
            }
            var result = Storage.DataStore.ModifyData(statements.ToArray());
            foreach(var identity in result.Identities) {
                identityAwaiters[identity.Tag - 1].Invoke(identity.Value);
            }
        }
        private void DeleteObject(object obj, IList<ModificationStatement> statements) {
            if(Storage.Mappings.TryGetValue(obj.GetType(), out var mapping)) {
                var statement = new DeleteStatement(mapping.Table, null);
                SetupUpdateDeleteStatement(statement, obj, mapping);
                statements.Add(statement);
            }
        }
        private void InsertObject(object obj, IList<ModificationStatement> statements, List<Action<object>> identityAwaiters) {
            if(Storage.Mappings.TryGetValue(obj.GetType(), out var mapping)) {
                var statement = new InsertStatement(mapping.Table, null);
                if(mapping.Table.PrimaryKey != null) {
                    foreach(var columnName in mapping.Table.PrimaryKey.Columns) {
                        var column = mapping.Table.GetColumn(columnName);
                        if(column.IsIdentity) {
                            identityAwaiters.Add(v => { mapping.SetKey(obj, v); });
                            statement.IdentityColumn = column.Name;
                            statement.IdentityColumnType = column.ColumnType;
                            statement.IdentityParameter = new ParameterValue(identityAwaiters.Count);
                            break;
                        }
                    }
                }
                SetupInsertUpdateStatement(statement, obj, mapping);
                statements.Add(statement);
            }
        }
        private void UpdateObject(object obj, IList<ModificationStatement> statements) {
            if(Storage.Mappings.TryGetValue(obj.GetType(), out var mapping)) {
                var statement = new UpdateStatement(mapping.Table, null);
                SetupUpdateDeleteStatement(statement, obj, mapping);
                SetupInsertUpdateStatement(statement, obj, mapping);
                statements.Add(statement);
            }
        }
        private void SetupUpdateDeleteStatement(ModificationStatement statement, object obj, DataStoreMapping mapping) {
            var criteria = new BinaryOperator(objectSpace.GetKeyPropertyName(obj.GetType()), objectSpace.GetKeyValue(obj));
            statement.Condition = SimpleDataStoreCriteriaVisitor.Process(criteria, mapping.Table, statement.Alias);
        }
        private void SetupInsertUpdateStatement(ModificationStatement statement, object obj, DataStoreMapping mapping) {
            var values = new object[mapping.Table.Columns.Count];
            mapping.Save(obj, values);
            for(int i = 0; i < values.Length; i++) {
                var column = mapping.Table.Columns[i];
                if(!column.IsIdentity) {
                    statement.Operands.Add(new QueryOperand(column, null));
                    statement.Parameters.Add(new OperandValue(values[i]));
                }
            }
        }
    }

    public class DataStoreMapping {
        public DBTable Table;
        public Func<object> Create;
        public Action<object, object[], IObjectSpace> Load;
        public Action<object, object[]> Save;
        public Action<object, object> SetKey;
        public IEnumerable<Column> RefColumns;
        public struct Column {
            public int Index;
            public Type Type;
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
            mAccount.Load = (obj, values, os) => {
                ((Account)obj).SetKey((string)values[0]);
                ((Account)obj).PublicName = (string)values[1];
            };
            mAccount.Save = (obj, values) => {
                values[0] = ((Account)obj).UserName;
                values[1] = ((Account)obj).PublicName;
            };
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
            mMessage.Load = (obj, values, os) => {
                var o = (Message)obj;
                o.SetKey((int)values[0]);
                o.Subject = (string)values[1];
                o.Body = (string)values[2];
                o.Sender = os.GetObjectByKey<Account>(values[3]);
                o.Recepient = os.GetObjectByKey<Account>(values[4]);
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
        }
    }

    class SimpleDataStoreCriteriaVisitor : ClientCriteriaVisitorBase {
        private DBTable table;
        private string alias;
        public SimpleDataStoreCriteriaVisitor(DBTable table, string alias) {
            this.table = table;
            this.alias = alias;
        }
        protected override CriteriaOperator Visit(OperandProperty theOperand) {
            return new QueryOperand(table.GetColumn(theOperand.PropertyName), alias);
        }
        public static CriteriaOperator Process(CriteriaOperator criteria, DBTable table, string alias) {
            return new SimpleDataStoreCriteriaVisitor(table, alias).Process(criteria);
        }
    }

#if false
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
#endif
}
