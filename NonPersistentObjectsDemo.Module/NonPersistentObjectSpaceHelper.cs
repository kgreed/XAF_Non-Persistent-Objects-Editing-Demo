using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;

namespace NonPersistentObjectsDemo.Module {

    public class NonPersistentObjectSpaceHelper : IDisposable {
        public static bool UseSharedAdditionalObjectSpace = false; //TODO: remove + unused members
        private XafApplication application;
        private IObjectSpace additionalObjectSpace;
        public List<Action<NonPersistentObjectSpace>> AdapterCreators { get; }

        public NonPersistentObjectSpaceHelper(XafApplication application) {
            this.application = application;
            this.AdapterCreators = new List<Action<NonPersistentObjectSpace>>();
            application.ObjectSpaceCreated += Application_ObjectSpaceCreated;
            if(UseSharedAdditionalObjectSpace) {
                additionalObjectSpace = CreatePersistentObjectSpace();
            }
        }
        public void Dispose() {
            application.ObjectSpaceCreated -= Application_ObjectSpaceCreated;
            if(additionalObjectSpace != null) {
                additionalObjectSpace.Dispose();
                additionalObjectSpace = null;
            }
        }
        private void Application_ObjectSpaceCreated(Object sender, ObjectSpaceCreatedEventArgs e) {
            if(e.ObjectSpace is NonPersistentObjectSpace) {
                NonPersistentObjectSpace npos = (NonPersistentObjectSpace)e.ObjectSpace;
                if(UseSharedAdditionalObjectSpace) {
                    npos.AdditionalObjectSpaces.Add(additionalObjectSpace);
                }
                else {
                    IObjectSpace persistentObjectSpace = CreatePersistentObjectSpace();
                    npos.AdditionalObjectSpaces.Add(persistentObjectSpace);
                    npos.NeedDisposeAdditionalObjectSpaces = true;
                }
                foreach(var adapterCreator in AdapterCreators) {
                    adapterCreator.Invoke(npos);
                }
            }
        }
        private IObjectSpace CreatePersistentObjectSpace() {
            return application.CreateObjectSpace(typeof(DevExpress.Persistent.BaseImpl.Note));
        }
    }
}
