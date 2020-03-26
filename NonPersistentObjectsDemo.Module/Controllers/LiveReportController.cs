using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using NonPersistentObjectsDemo.Module.BusinessObjects;

namespace NonPersistentObjectsDemo.Module.Controllers {
    public class LiveReportController : ObjectViewController<ObjectView, LiveReport> {
        protected override void OnActivated() {
            base.OnActivated();
            var npos = ObjectSpace as NonPersistentObjectSpace;
            if(npos != null) {
                npos.NeedReloadAdditionalObjectSpaces = true;
            }
            //var filterController = Frame.GetController<FilterController>();
            //if(filterController != null) {
            //    filterController.AllowFilterNonPersistentObjects = true;
            //}
        }
    }
}
