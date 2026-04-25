// Avansas site JavaScript

function getCsrfToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

// ─── Son Görüntülenen Ürünler ───
const RecentlyViewed = {
    key: 'avansas_recently_viewed',
    max: 10,
    get() {
        try { return JSON.parse(localStorage.getItem(this.key) || '[]'); } catch { return []; }
    },
    add(product) {
        if (!product?.id) return;
        let items = this.get().filter(p => p.id !== product.id);
        items.unshift({ id: product.id, name: product.name, slug: product.slug, price: product.price, image: product.image });
        localStorage.setItem(this.key, JSON.stringify(items.slice(0, this.max)));
    },
    render(containerId) {
        const el = document.getElementById(containerId);
        if (!el) return;
        const items = this.get();
        if (!items.length) { el.style.display = 'none'; return; }
        el.style.display = '';
        const inner = el.querySelector('.rv-items');
        if (!inner) return;
        inner.innerHTML = items.map(p => `
            <div class="col-6 col-md-4 col-lg-2">
                <a href="/urun/${p.slug}" class="text-decoration-none">
                    <div class="bg-white rounded-3 p-2 text-center shadow-sm h-100">
                        <img src="${p.image || '/images/no-image.png'}" class="img-fluid mb-2" style="height:80px;object-fit:contain;" alt="${p.name}" />
                        <div class="small fw-semibold text-dark text-truncate">${p.name}</div>
                        <div class="text-primary fw-bold small">${p.price}₺</div>
                    </div>
                </a>
            </div>`).join('');
    }
};

// ─── Karşılaştırma ───
const Compare = {
    async add(productId) {
        const res = await fetch('/karsilastir/ekle', { method: 'POST', headers: {'Content-Type':'application/x-www-form-urlencoded'}, body: `productId=${productId}` });
        const data = await res.json();
        if (data.success) {
            this.updateBadge(data.count);
        } else { alert(data.message); }
    },
    async remove(productId) {
        const res = await fetch('/karsilastir/sil', { method: 'POST', headers: {'Content-Type':'application/x-www-form-urlencoded'}, body: `productId=${productId}` });
        const data = await res.json();
        if (data.success) this.updateBadge(data.count);
    },
    updateBadge(count) {
        const el = document.getElementById('compareCount');
        if (el) { el.textContent = count; el.style.display = count > 0 ? '' : 'none'; }
    },
    async loadCount() {
        try {
            const res = await fetch('/karsilastir/adet');
            const count = await res.json();
            this.updateBadge(count);
        } catch {}
    }
};

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.add-to-cart').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const productId = parseInt(btn.dataset.productId);
            if (!productId) return;
            btn.disabled = true;
            const original = btn.innerHTML;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Ekleniyor...';
            try {
                const res = await fetch('/sepet/add', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ productId, quantity: 1 })
                });
                const data = await res.json();
                if (data.success) {
                    const badge = document.getElementById('cartCount');
                    if (badge) { badge.textContent = data.itemCount; badge.style.display = ''; }
                    btn.innerHTML = '<i class="fas fa-check me-1"></i> Eklendi!';
                    btn.classList.replace('btn-primary', 'btn-success');
                    setTimeout(() => { btn.innerHTML = original; btn.classList.replace('btn-success', 'btn-primary'); btn.disabled = false; }, 2000);
                } else {
                    alert(data.message);
                    btn.innerHTML = original; btn.disabled = false;
                }
            } catch { btn.innerHTML = original; btn.disabled = false; }
        });
    });
});
