using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace NonPersistentObjectsDemo.Module.BusinessObjects {

    [VisibleInReports]
    [NavigationItem("Newsflash")]
    [DefaultProperty(nameof(Title))]
    [DevExpress.ExpressApp.ConditionalAppearance.Appearance("", Enabled = false, Criteria = "Not IsNewObject(This)", TargetItems = "*")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Article : NonPersistentObjectBase {
        private int id;
        [Browsable(false)]
        [DevExpress.ExpressApp.Data.Key]
        public int ID {
            get { return id; }
            set { id = value; }
        }
        private Contact _Author;
        public Contact Author {
            get { return _Author; }
            set { SetPropertyValue(nameof(Author), ref _Author, value); }
        }
        private string _Title;
        public string Title {
            get { return _Title; }
            set { SetPropertyValue<string>(nameof(Title), ref _Title, value); }
        }
        private string _Content;
        [FieldSize(-1)]
        public string Content {
            get { return _Content; }
            set { SetPropertyValue<string>(nameof(Content), ref _Content, value); }
        }
    }

    //[VisibleInReports]
    //[NavigationItem("Newsflash")]
    [DefaultProperty(nameof(FullName))]
    [DevExpress.ExpressApp.ConditionalAppearance.Appearance("", Enabled = false, TargetItems = "*")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class Contact : NonPersistentObjectBase {
        internal Contact() { }
        private string userName;
        [DevExpress.ExpressApp.Data.Key]
        public string UserName {
            get { return userName; }
            set { userName = value; }
        }
        public void SetKey(string userName) {
            this.userName = userName;
        }
        private string fullName;
        public string FullName {
            get { return fullName; }
            set { SetPropertyValue(nameof(FullName), ref fullName, value); }
        }
    }

    class NonPersistentObjectSimpleFilteringAdapter {
        private NonPersistentObjectSpace objectSpace;
        private static List<Contact> contacts;
        private static List<Article> articles;

        public NonPersistentObjectSimpleFilteringAdapter(NonPersistentObjectSpace npos) {
            this.objectSpace = npos;
            objectSpace.ObjectsGetting += ObjectSpace_ObjectsGetting;
            objectSpace.CustomCommitChanges += ObjectSpace_CustomCommitChanges;
        }
        private void ObjectSpace_ObjectsGetting(object sender, ObjectsGettingEventArgs e) {
            if(e.ObjectType == typeof(Article) || e.ObjectType == typeof(Contact)) {
                var collection = new DynamicCollection(objectSpace, e.ObjectType, e.Criteria, e.Sorting, e.InTransaction);
                collection.ObjectsGetting += DynamicCollection_ObjectsGetting;
                e.Objects = collection;
            }
        }
        private void DynamicCollection_ObjectsGetting(object sender, DynamicObjectsGettingEventArgs e) {
            if(e.ObjectType == typeof(Article)) {
                e.Objects = articles;
            }
            else if(e.ObjectType == typeof(Contact)) {
                e.Objects = contacts;
            }
            e.ShapeRawData = true;
        }
        private void ObjectSpace_CustomCommitChanges(object sender, HandledEventArgs e) {
            foreach(var obj in objectSpace.GetObjectsToSave(false)) {
                if(obj is Article article) {
                    if(objectSpace.IsNewObject(obj)) {
                        article.ID = articles.Count;
                        articles.Add(article);
                    }
                }
            }
        }

        #region DemoData
        static NonPersistentObjectSimpleFilteringAdapter() {
            contacts = new List<Contact>();
            articles = new List<Article>();
            var gen = new GenHelper();
            var ids = new List<string>();
            for(int i = 0; i < 200; i++) {
                var id = gen.MakeTosh(20);
                ids.Add(id);
                contacts.Add(new Contact() {
                    UserName = id,
                    FullName = gen.GetFullName()
                });
            }
            for(int i = 0; i < 5000; i++) {
                var id1 = gen.Next(ids.Count);
                var id2 = gen.Next(ids.Count - 1);
                articles.Add(new Article() {
                    ID = i,
                    Title = GenHelper.ToTitle(gen.MakeBlah(gen.Next(7))),
                    Content = gen.MakeBlahBlahBlah(5 + gen.Next(100), 7),
                    Author = contacts[id1]
                });
            }
        }
        #endregion
    }
}
