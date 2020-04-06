using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Security.Strategy;
using DevExpress.Xpo;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using NonPersistentObjectsDemo.Module.BusinessObjects;
using System.Collections.Generic;

namespace NonPersistentObjectsDemo.Module.DatabaseUpdate {
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion) {
        }
        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            //CreateSecurityObjects();
            CreateTQBFDemoObjects();
            ObjectSpace.CommitChanges(); //This line persists created object(s).
        }

        public override void UpdateDatabaseBeforeUpdateSchema() {
            base.UpdateDatabaseBeforeUpdateSchema();
            //if(CurrentDBVersion < new Version("1.1.0.0") && CurrentDBVersion > new Version("0.0.0.0")) {
            //    RenameColumn("DomainObject1Table", "OldColumnName", "NewColumnName");
            //}
        }

        #region TQBF

        static string[] productNames = { "Fresh Flesh", "Soaked Souls", "Elixir of Eternity", "Bones Barbeque", "Cranium Cake" };

        private void CreateTQBFDemoObjects() {
            var rnd = new Random(8573);
            IList<Product> products = null;
            if(ObjectSpace.GetObjectsCount(typeof(Product), null) == 0) {
                products = new List<Product>();
                for(var i = 0; i < productNames.Length; i++) {
                    var product = ObjectSpace.CreateObject<Product>();
                    product.Name = productNames[i];
                    product.Price = (80 + rnd.Next(2000)) * 0.01m;
                    products.Add(product);
                }
            }
            if(ObjectSpace.GetObjectsCount(typeof(Order), null) == 0) {
                if(products == null) {
                    products = ObjectSpace.GetObjects<Product>();
                }
                var now = DateTime.Now;
                for(var i = 0; i < 1000; i++) {
                    now = now.AddMinutes(-(1 + rnd.Next(20)));
                    var order = ObjectSpace.CreateObject<Order>();
                    order.Date = now;
                    var dnum = 1 + rnd.Next(21);
                    for(var j = 0; j < dnum; j++) {
                        var product = products[rnd.Next(products.Count)];
                        var detail = order.Details.FirstOrDefault(d => d.Product == product);
                        if(detail == null) {
                            detail = ObjectSpace.CreateObject<OrderLine>();
                            detail.Product = product;
                            order.Details.Add(detail);
                        }
                        detail.Quantity++;
                    }
                    order.Address = string.Format("{0} W {1} St", (1 + rnd.Next(15)) * 100 + 1 + rnd.Next(30), rnd.Next(100));
                    order.Total = order.Details.Sum(d => d.Quantity * d.Product.Price);
                    order.Status = CalcStatus(now, rnd);
                }
            }
        }
        private OrderStatus CalcStatus(DateTime now, Random rnd) {
            var delay = DateTime.Now.Subtract(now);
            if(delay.TotalMinutes > 120) {
                return (rnd.Next(30) == 0) ? OrderStatus.Canceled : OrderStatus.Delivered;
            }
            else {
                if(delay.TotalMinutes > 20) {
                    if(rnd.Next(30) == 0) {
                        return OrderStatus.Canceled;
                    }
                    else {
                        if(delay.TotalMinutes > 30) {
                            if(delay.TotalMinutes < 60 && rnd.Next(7) == 0) {
                                return OrderStatus.Confirmed;
                            }
                            else {
                                return (rnd.Next(5) == 0) ? OrderStatus.Ready : OrderStatus.Delivered;
                            }
                        }
                        else {
                            return (rnd.Next(4) == 0) ? OrderStatus.Ready : OrderStatus.Confirmed;
                        }
                    }
                }
                else {
                    return (rnd.Next(3) == 0) ? OrderStatus.Confirmed : OrderStatus.Pending;
                }
            }
        }
        #endregion

        #region Security

        private void CreateSecurityObjects() {
            PermissionPolicyUser sampleUser = ObjectSpace.FindObject<PermissionPolicyUser>(new BinaryOperator("UserName", "User"));
            if(sampleUser == null) {
                sampleUser = ObjectSpace.CreateObject<PermissionPolicyUser>();
                sampleUser.UserName = "User";
                sampleUser.SetPassword("");
            }
            PermissionPolicyRole defaultRole = CreateDefaultRole();
            sampleUser.Roles.Add(defaultRole);

            PermissionPolicyUser userAdmin = ObjectSpace.FindObject<PermissionPolicyUser>(new BinaryOperator("UserName", "Admin"));
            if(userAdmin == null) {
                userAdmin = ObjectSpace.CreateObject<PermissionPolicyUser>();
                userAdmin.UserName = "Admin";
                // Set a password if the standard authentication type is used
                userAdmin.SetPassword("");
            }
            // If a role with the Administrators name doesn't exist in the database, create this role
            PermissionPolicyRole adminRole = ObjectSpace.FindObject<PermissionPolicyRole>(new BinaryOperator("Name", "Administrators"));
            if(adminRole == null) {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
            }
            adminRole.IsAdministrative = true;
            userAdmin.Roles.Add(adminRole);
        }

        private PermissionPolicyRole CreateDefaultRole() {
            PermissionPolicyRole defaultRole = ObjectSpace.FindObject<PermissionPolicyRole>(new BinaryOperator("Name", "Default"));
            if(defaultRole == null) {
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

				defaultRole.AddObjectPermission<PermissionPolicyUser>(SecurityOperations.Read, "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
				defaultRole.AddMemberPermission<PermissionPolicyUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
				defaultRole.AddMemberPermission<PermissionPolicyUser>(SecurityOperations.Write, "StoredPassword", "[Oid] = CurrentUserId()", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.ReadWriteAccess, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, SecurityPermissionState.Allow);
				defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }

        #endregion
    }
}
