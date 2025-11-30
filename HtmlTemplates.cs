namespace ShopeeServer
{
    public static class HtmlTemplates
    {
        public static string Index = @"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no'>
    <title>Shopee WMS Pro</title>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css'>
    <script src='https://unpkg.com/vue@3/dist/vue.global.js'></script>
    <style>
        body { background-color: #f4f6f8; padding-bottom: 100px; font-size: 14px; font-family: -apple-system, sans-serif; }
        
        /* CSS Giao diện */
        .card-item { background: white; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); margin-bottom: 10px; border: 1px solid #eee; overflow: hidden; }
        .card-body-custom { padding: 12px; display: flex; align-items: flex-start; position: relative; }
        
        .img-box { position: relative; width: 100px; height: 100px; flex-shrink: 0; margin-right: 12px; cursor: pointer; }
        .img-thumb { width: 100%; height: 100%; object-fit: cover; border-radius: 6px; border: 1px solid #eee; }
        .zoom-icon { position: absolute; bottom: 0; right: 0; background: rgba(0,0,0,0.6); color: white; font-size: 10px; padding: 2px 4px; border-radius: 4px 0 4px 0; }
        
        .info-box { flex-grow: 1; min-width: 0; display: flex; flex-direction: column; }
        .product-name { font-weight: 700; color: #222; margin-bottom: 4px; line-height: 1.3; font-size: 13px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
        .variation-badge { font-size: 12px; color: #e65100; background: #fff3e0; border: 1px solid #ffe0b2; padding: 2px 6px; border-radius: 4px; width: fit-content; margin-bottom: 4px; font-weight: 500; }
        .location-badge { font-size: 11px; color: #1565c0; background: #e3f2fd; padding: 1px 6px; border-radius: 4px; width: fit-content; font-weight: bold; border: 1px solid #bbdefb; }
        
        .qty-box { text-align: right; padding-left: 8px; display: flex; flex-direction: column; align-items: flex-end; min-width: 40px; }
        .qty-text { font-size: 25px; font-weight: 700; color: #333; }
        .qty-text.red { color: #d32f2f; }
        
        /* Header đơn hàng */
        .order-header { padding: 12px; background: white; border-bottom: 1px solid #f0f0f0; cursor: pointer; display: flex; justify-content: space-between; align-items: center; border-radius: 8px; margin-bottom: 8px; box-shadow: 0 1px 2px rgba(0,0,0,0.05); }
        .order-header.active { background: #e8f5e9; border: 1px solid #81c784; margin-bottom: 0; border-radius: 8px 8px 0 0; }
        .highlight-sn { color: #d32f2f; font-weight: 900; background: #ffebee; padding: 0 2px; border-radius: 2px; }
        .order-detail-box { background: #fafafa; border: 1px solid #81c784; border-top: none; border-radius: 0 0 8px 8px; padding: 10px; margin-bottom: 10px; }
        
        /* Picking */
        .picking-group-header { background: #ff9800; color: white; padding: 8px 12px; font-weight: bold; border-radius: 6px; margin-top: 15px; margin-bottom: 8px; }
        .picking-card.done { opacity: 0.5; filter: grayscale(100%); }
        .picking-orders { background: #fff8e1; padding: 10px; display: none; }
        .picking-orders.show { display: block; }
        
        .tag-sn { background: white; border: 1px solid #ffd54f; padding: 3px 8px; border-radius: 4px; margin-right: 5px; display: inline-block; font-family: monospace; }
        .big-checkbox { width: 24px; height: 24px; accent-color: #2e7d32; margin-top: 2px; }
        .btn-float { position: fixed; bottom: 20px; left: 50%; transform: translateX(-50%); width: 90%; max-width: 500px; padding: 14px; border-radius: 50px; font-weight: bold; box-shadow: 0 4px 15px rgba(0,0,0,0.3); z-index: 1000; border: none; }

        /* Modal */
        .modal-main-img { width: 100%; height: 250px; object-fit: contain; border-radius: 8px; background: #f8f9fa; border: 1px solid #eee; display: block; margin: 0 auto; }
        .comp-item { display: flex; align-items: center; padding: 8px 0; border-bottom: 1px solid #f0f0f0; cursor: pointer; }
        .comp-item.active { background-color: #e3f2fd; border-radius: 6px; padding: 8px; border: 1px solid #90caf9; }
        .comp-img { width: 40px; height: 40px; object-fit: cover; border-radius: 4px; border: 1px solid #ddd; margin-right: 10px; }
        .comp-stock { font-weight: bold; color: #198754; font-size: 14px; white-space: nowrap; }
        .comp-stock.low { color: #d32f2f; }
    </style>
</head>
<body>
<div id='app' class='container py-2' style='max-width:600px'>
    
    <div v-if='!loaded' class='text-center mt-5'>
        <div class='spinner-border text-primary' role='status'></div>
        <p class='mt-2'>Đang kết nối máy chủ...</p>
    </div>

    <div v-else>
        <div class='d-flex justify-content-between align-items-center mb-3 bg-white p-3 rounded shadow-sm sticky-top'>
            <span class='fw-bold text-primary h5 mb-0'>KHO HÀNG</span>
            <div class='d-flex align-items-center gap-2'>
                <input type='range' class='form-range' style='width:60px' min='0.8' max='1.3' step='0.1' v-model='zoomLevel' @input='updateZoom'>
                <button class='btn btn-sm btn-light border' @click='fetchData'><i class='bi bi-arrow-clockwise'></i></button>
            </div>
        </div>

        <div v-if='currentView === ""manager""'>
            <ul class='nav nav-pills nav-fill mb-3 bg-white p-1 rounded shadow-sm'>
                <li class='nav-item'><a class='nav-link' :class='{active: tab===""unprocessed""}' @click='tab=""unprocessed""'>Chờ xử lý ({{unprocessedOrders.length}})</a></li>
                <li class='nav-item'><a class='nav-link' :class='{active: tab===""processed""}' @click='tab=""processed""'>Đã xử lý ({{processedOrders.length}})</a></li>
            </ul>

            <div class='d-flex justify-content-between mb-2' v-if='tab===""unprocessed""'>
                <button class='btn btn-sm btn-white border shadow-sm' @click='sortDesc = !sortDesc'>
                    <i class='bi' :class='sortDesc ? ""bi-sort-down"" : ""bi-sort-up""'></i> {{sortDesc ? 'Mới nhất' : 'Cũ nhất'}}
                </button>
                <button class='btn btn-sm fw-bold shadow-sm' :class='isBatchMode ? ""btn-danger"" : ""btn-warning""' @click='toggleBatchMode'>
                    {{ isBatchMode ? '❌ Hủy' : '📦 Gom đơn' }}
                </button>
            </div>

            <div v-for='order in filteredOrders' :key='order.OrderId'>
                <div class='order-header' :class='{active: openOrderId === order.OrderId}' @click='toggleOrder(order.OrderId)'>
                    <div class='d-flex align-items-center'>
                        <input v-if='isBatchMode' type='checkbox' class='form-check-input me-3 big-checkbox' v-model='order.Selected' @click.stop>
                        <span style='font-family:monospace;font-size:1.1em'>#{{order.OrderId.slice(0, -4)}}<span class='highlight-sn'>{{order.OrderId.slice(-4)}}</span>
                    </div>
                    <span class='badge bg-secondary'>{{order.Items.length}} món</span>
                </div>

                <div v-if='openOrderId === order.OrderId' class='order-detail-box'>
                    <div v-for='item in order.Items' class='card-item border-0 mb-1'>
                        <div class='card-body-custom'>
                            <div class='img-box' @click.stop='showProductModal(item)'>
                                <img :src='item.ImageUrl' class='img-thumb'>
                                <div class='zoom-icon'><i class='bi bi-eye-fill'></i></div>
                            </div>
                            <div class='info-box'>
                                <div class='product-name'>{{item.ProductName}}</div>
                                <div class='variation-badge'>{{item.ModelName}}</div>
                                <div class='location-badge'>{{item.Shelf}}{{item.Level}}{{item.Box}}</div>
                            </div>
                            <div class='qty-box'><span class='qty-text' :class='{red: item.Quantity > 1}'>x{{item.Quantity}}</span></div>
                        </div>
                    </div>
                    <button v-if='!isBatchMode' class='btn btn-danger w-100 mt-2 fw-bold' @click='shipOrder(order.OrderId)'>CHUẨN BỊ ĐƠN</button>
                </div>
            </div>

            <button v-if='isBatchMode && selectedCount > 0' class='btn btn-warning btn-float text-white' @click='startPicking'>
                BẮT ĐẦU SOẠN ({{selectedCount}}) <i class='bi bi-arrow-right'></i>
            </button>
        </div>

        <div v-if='currentView === ""picking""'>
            <div class='sticky-top bg-white p-3 shadow-sm d-flex justify-content-between mb-3'>
                <button class='btn btn-outline-secondary btn-sm' @click='currentView=""manager""'>Thoát</button>
                <span class='fw-bold text-warning'>NHẶT HÀNG</span>
                <span class='badge bg-warning text-dark'>{{batchItems.length}} món</span>
            </div>

            <div v-for='(group, loc) in groupedBatch' :key='loc'>
                <div class='picking-group-header'><i class='bi bi-geo-alt-fill me-1'></i> {{loc}}</div>
                <div v-for='item in group' class='card-item picking-card' :class='{done: item.Picked}'>
                    <div class='card-body-custom'>
                        <div class='img-box' @click.stop='showProductModal(item)'>
                            <img :src='item.ImageUrl' class='img-thumb'>
                            <div class='zoom-icon'><i class='bi bi-eye-fill'></i></div>
                        </div>
                        <div class='info-box' @click='item.ShowDetail = !item.ShowDetail'>
                            <div class='product-name'>{{item.ProductName}}</div>
                            <div class='variation-badge'>{{item.ModelName}}</div>
                            <div class='text-muted small mt-1' v-if='!item.ShowDetail'>Xem {{item.OrderIds.length}} đơn <i class='bi bi-caret-down-fill'></i></div>
                        </div>
                        <div class='qty-box'>
                            <span class='qty-text' :class='{red: item.TotalQty > 1}'>{{item.TotalQty}}</span>
                            <input type='checkbox' class='big-checkbox mt-2' v-model='item.Picked' @click.stop>
                        </div>
                    </div>
                    <div class='picking-orders' :class='{show: item.ShowDetail}'>
                        <strong>Đơn cần lấy:</strong><div class='mt-1'><span v-for='id in item.OrderIds' class='tag-sn'>...{{id}}</span></div>
                    </div>
                </div>
            </div>
        </div>

        <div class='modal fade' id='productModal' tabindex='-1'>
            <div class='modal-dialog modal-dialog-centered'>
                <div class='modal-content'>
                    <div class='modal-header border-0 pb-0'>
                        <button type='button' class='btn-close ms-auto' data-bs-dismiss='modal'></button>
                    </div>
                    <div class='modal-body pt-0'>
                        <div v-if='loadingModal' class='text-center text-warning fw-bold mb-2'>Đang tải...</div>
                        <img :src='modalItem.img' class='modal-main-img mb-3'>
                        <div class='text-center mb-3'>
                            <h5 class='fw-bold text-danger mb-1'>{{modalItem.name}}</h5>
                            <div class='badge bg-primary fs-6 p-2 mt-1'>Kho: {{modalItem.stock}}</div>
                        </div>
                        <div class='text-start border-top pt-2'>
                            <small class='fw-bold text-secondary'>PHÂN LOẠI KHÁC:</small>
                            <div class='list-group list-group-flush mt-2' style='max-height:250px;overflow-y:auto'>
                                <div v-for='v in variations' class='comp-item' :class='{active: v.name === modalItem.name}' @click='selectVariation(v)'>
                                    <img :src='v.img' class='comp-img'>
                                    <div class='flex-grow-1'>
                                        <div class='fw-bold' :class='v.name === modalItem.name ? ""text-primary"" : ""text-dark""'>{{v.name}}</div>
                                    </div>
                                    <div class='comp-stock' :class='{low: v.stock < 5}'>{{v.stock}}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<script src='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js'></script>
<script>
    const { createApp } = Vue;
    createApp({
        data() {
            return {
                loaded: false,
                orders: [], tab: 'unprocessed', currentView: 'manager', isBatchMode: false, sortDesc: true, openOrderId: null, batchItems: [], users: ['Kho 1', 'Kho 2'], modalItem: {}, variations: [], loadingModal: false, zoomLevel: 1.0
            }
        },
        computed: {
            unprocessedOrders() { return this.orders.filter(o => o.Status === 0); },
            processedOrders() { return this.orders.filter(o => o.Status === 1); },
            filteredOrders() {
                let list = this.tab === 'unprocessed' ? this.unprocessedOrders : this.processedOrders;
                return list.sort((a,b) => this.sortDesc ? b.CreatedAt - a.CreatedAt : a.CreatedAt - b.CreatedAt);
            },
            selectedCount() { return this.orders.filter(o => o.Selected).length; },
            groupedBatch() {
                const groups = {};
                this.batchItems.forEach(i => {
                    const key = i.Location || 'Khác';
                    if(!groups[key]) groups[key] = [];
                    groups[key].push(i);
                });
                return Object.keys(groups).sort().reduce((obj, key) => { obj[key] = groups[key]; return obj; }, {});
            }
        },
        mounted() {
            this.fetchData();
            setInterval(this.fetchData, 10000);
            this.updateZoom();
        },
        methods: {
            async fetchData() {
                try {
                    const res = await fetch('/api/data');
                    const data = await res.json();
                    const selectedIds = new Set(this.orders.filter(o => o.Selected).map(o => o.OrderId));
                    this.orders = data.map(o => ({...o, Selected: selectedIds.has(o.OrderId)}));
                    this.loaded = true;
                } catch(e) {}
            },
            updateZoom() { document.body.style.zoom = this.zoomLevel; },
            toggleBatchMode() { this.isBatchMode = !this.isBatchMode; this.orders.forEach(o => o.Selected = false); this.openOrderId = null; },
            toggleOrder(id) {
                if (this.isBatchMode) { const o = this.orders.find(x => x.OrderId === id); if(o) o.Selected = !o.Selected; }
                else { this.openOrderId = (this.openOrderId === id) ? null : id; }
            },
            async assignUser(id, u) { await fetch(`/api/assign?id=${id}&user=${encodeURIComponent(u)}`, {method: 'POST'}); this.fetchData(); },
            async shipOrder(id) { if(!confirm('In đơn?')) return; await fetch(`/api/ship?id=${id}`, {method: 'POST'}); this.openOrderId = null; this.fetchData(); },
            
            async showProductModal(item) {
                this.loadingModal = true;
                this.modalItem = { name: item.ModelName, img: item.ImageUrl, stock: '...' };
                this.variations = [];
                new bootstrap.Modal(document.getElementById('productModal')).show();

                try {
                    const res = await fetch('/api/product?id=' + item.ItemId);
                    const data = await res.json();
                    if(data.success) {
                        this.variations = data.variations;
                        const current = this.variations.find(v => v.name === item.ModelName);
                        if(current) this.modalItem = { ...current, name: current.name, img: current.img, stock: current.stock };
                    }
                } catch(e) {}
                this.loadingModal = false;
            },
            selectVariation(v) { this.modalItem = { name: v.name, img: v.img, stock: v.stock }; },
            startPicking() {
                const selected = this.orders.filter(o => o.Selected);
                const agg = {};
                selected.forEach(order => {
                    order.Items.forEach(item => {
                        const key = item.ModelName + item.ParsedLocation; 
                        if(!agg[key]) agg[key] = { ItemId: item.ItemId, ProductName: item.ProductName, ModelName: item.ModelName, ImageUrl: item.ImageUrl, Location: item.ParsedLocation, SKU: item.SKU, TotalQty: 0, OrderIds: [], Picked: false, ShowDetail: false };
                        agg[key].TotalQty += item.Quantity;
                        agg[key].OrderIds.push(order.OrderId.slice(-4));
                    });
                });
                this.batchItems = Object.values(agg);
                this.currentView = 'picking';
            }
        }
    }).mount('#app');
</script>
</body>
</html>";
    }
}