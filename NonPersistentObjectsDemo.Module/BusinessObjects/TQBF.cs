using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace NonPersistentObjectsDemo.Module.BusinessObjects {

    [DevExpress.ExpressApp.DC.XafDefaultProperty(nameof(Address))]
    [NavigationItem("TheQuickBrownFox")]
    public class Order : BaseObject {
        public Order(Session session) : base(session) { }

        private DateTime _Date;
        public DateTime Date {
            get { return _Date; }
            set { SetPropertyValue<DateTime>(nameof(Date), ref _Date, value); }
        }
        private string _Address;
        public string Address {
            get { return _Address; }
            set { SetPropertyValue<string>(nameof(Address), ref _Address, value); }
        }
        private decimal _Total;
        public decimal Total {
            get { return _Total; }
            set { SetPropertyValue<decimal>(nameof(Total), ref _Total, value); }
        }
        private OrderStatus _Status;
        public OrderStatus Status {
            get { return _Status; }
            set { SetPropertyValue<OrderStatus>(nameof(Status), ref _Status, value); }
        }
        [Aggregated]
        [Association]
        public XPCollection<OrderLine> Details {
            get { return GetCollection<OrderLine>(nameof(Details)); }
        }
    }

    public enum OrderStatus {
        Pending, Confirmed, Ready, Delivered, Canceled
    }

    [DevExpress.ExpressApp.DC.XafDefaultProperty(nameof(Product))]
    public class OrderLine : BaseObject {
        public OrderLine(Session session) : base(session) { }

        private Order _Order;
        [Association]
        public Order Order {
            get { return _Order; }
            set { SetPropertyValue<Order>(nameof(Order), ref _Order, value); }
        }
        private Product _Product;
        public Product Product {
            get { return _Product; }
            set { SetPropertyValue<Product>(nameof(Product), ref _Product, value); }
        }
        private int _Quantity;
        public int Quantity {
            get { return _Quantity; }
            set { SetPropertyValue<int>(nameof(Quantity), ref _Quantity, value); }
        }
    }

    [NavigationItem("TheQuickBrownFox")]
    public class Product : BaseObject {
        public Product(Session session) : base(session) { }

        private string _Name;
        public string Name {
            get { return _Name; }
            set { SetPropertyValue<string>(nameof(Name), ref _Name, value); }
        }
        private decimal _Price;
        public decimal Price {
            get { return _Price; }
            set { SetPropertyValue<decimal>(nameof(Price), ref _Price, value); }
        }
    }

    [NavigationItem("TheQuickBrownFox")]
    [DevExpress.ExpressApp.DC.DomainComponent]
    public class LiveReport : NonPersistentObjectBase {
        private Guid id;
        [Browsable(false)]
        [DevExpress.ExpressApp.Data.Key]
        public Guid ID {
            get { return id; }
        }
        public void SetKey(Guid id) {
            this.id = id;
        }
        public string Name { get; private set; }
        public void SetName(string name) {
            this.Name = name;
        }
        [Browsable(false)]
        public OrderStatus Status { get; private set; }
        public void SetStatus(OrderStatus status) {
            this.Status = status;
        }
        [Browsable(false)]
        public int Period { get; private set; }
        public void SetPeriod(int period) {
            this.Period = period;
        }
        private int? _Count;
        public int Count {
            get {
                if(!_Count.HasValue && ObjectSpace != null) {
                    var pos = ((NonPersistentObjectSpace)ObjectSpace).AdditionalObjectSpaces.FirstOrDefault(os => os.IsKnownType(typeof(Order)));
                    if(pos != null) {
                        _Count = Convert.ToInt32(pos.Evaluate(typeof(Order), CriteriaOperator.Parse("Count()"), Criteria));
                    }
                }
                return _Count.Value;
            }
        }
        private decimal? _Total;
        public decimal Total {
            get {
                if(!_Total.HasValue && ObjectSpace != null) {
                    var pos = ((NonPersistentObjectSpace)ObjectSpace).AdditionalObjectSpaces.FirstOrDefault(os => os.IsKnownType(typeof(Order)));
                    if(pos != null) {
                        _Total = Convert.ToDecimal(pos.Evaluate(typeof(Order), CriteriaOperator.Parse("Sum([Total])"), Criteria));
                    }
                }
                return _Total.Value;
            }
        }
        private CriteriaOperator Criteria {
            get {
                return CriteriaOperator.Parse("DateDiffDay([Date], Now()) <= ? And [Status] = ?", Period, Status);
            }
        }
        private IList<Order> _Orders;
        public IList<Order> Orders {
            get {
                if(_Orders == null && ObjectSpace != null) {
                    _Orders = ObjectSpace.GetObjects<Order>(Criteria).ToArray();
                }
                return _Orders;
            }
        }
        private Order _LatestOrder;
        public Order LatestOrder {
            get {
                if(_LatestOrder == null && ObjectSpace != null) {
                    _LatestOrder = Orders.OrderBy(o => o.Date).FirstOrDefault();
                }
                return _LatestOrder;
            }
        }
    }

    public class LiveReportFactory : NonPersistentObjectFactoryBase {
        class LiveReportData {
            public string Name;
            public OrderStatus Status;
            public int Period;
        }
        private static Dictionary<Guid, LiveReportData> objectData;
        static LiveReportFactory() {
            objectData = new Dictionary<Guid, LiveReportData>();
            objectData.Add(Guid.NewGuid(), new LiveReportData() { Name = "Tentative", Status = OrderStatus.Pending, Period = 1 });
            objectData.Add(Guid.NewGuid(), new LiveReportData() { Name = "To produce", Status = OrderStatus.Confirmed, Period = 100 });
            objectData.Add(Guid.NewGuid(), new LiveReportData() { Name = "To deliver", Status = OrderStatus.Ready, Period = 100 });
            objectData.Add(Guid.NewGuid(), new LiveReportData() { Name = "Canceled this week", Status = OrderStatus.Canceled, Period = 7 });
            objectData.Add(Guid.NewGuid(), new LiveReportData() { Name = "Delivered this week", Status = OrderStatus.Delivered, Period = 7 });
        }

        public override object GetObjectByKey(Type objectType, object key) {
            if(typeof(LiveReport) == objectType) {
                LiveReport obj = null;
                var data = objectData[(Guid)key];
                if(data != null) {
                    obj = new LiveReport();
                    obj.SetKey((Guid)key);
                    obj.SetName(data.Name);
                    obj.SetPeriod(data.Period);
                    obj.SetStatus(data.Status);
                }
                return obj;
            }
            throw new NotImplementedException();
        }
        public override IEnumerable GetObjectKeys(Type objectType, CriteriaOperator criteria, IList<SortProperty> sorting) {
            if(typeof(LiveReport) == objectType) {
                return objectData.Keys.ToArray();
            }
            throw new NotImplementedException();
        }
    }
}
