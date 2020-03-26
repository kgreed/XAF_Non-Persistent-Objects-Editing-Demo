using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using NonPersistentObjectsDemo.Module.BusinessObjects;

namespace NonPersistentObjectsDemo.Module.Controllers {
    public class PostOfficeController : ObjectViewController<ObjectView, object> {
        protected override void OnActivated() {
            base.OnActivated();
            //if(View.ObjectTypeInfo.Type == typeof(Account) || View.ObjectTypeInfo.Type == typeof(Message)) {
            //    var filterController = Frame.GetController<FilterController>();
            //    if(filterController != null) {
            //        filterController.AllowFilterNonPersistentObjects = true;
            //    }
            //}
        }
    }
}
