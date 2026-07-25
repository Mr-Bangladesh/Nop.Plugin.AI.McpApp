namespace Nop.Plugin.AI.McpApp.Mcp.Resources;

public static class CartWidgetHtml
{
    public const string Markup = """
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width,initial-scale=1" />
      <style>
        body { margin:0; font-family: system-ui, sans-serif; padding:12px; }
        table{width:100%;border-collapse:collapse}
        th,td{padding:8px;border-bottom:1px solid #eee;text-align:left}
        img{max-width:64px;border-radius:4px}
        .total{font-weight:700;text-align:right;margin-top:12px}
      </style>
    </head>
    <body>
      <div id="root">
        <div id="placeholder">Waiting for cart data…</div>
      </div>
      <script type="module">
        import { App } from "https://esm.sh/@modelcontextprotocol/ext-apps@1.7.1?deps=zod@3.25.76";

        const app = new App({ name: 'cart-view', version: '1.0.0' });

        function render(result){
          const products = result.structuredContent?.products ?? [];
          const total = result.structuredContent?.total ?? null;
          const root = document.getElementById('root');
          if(!products || products.length === 0){
            root.innerHTML = '<div>No items in cart.</div>';
            return;
          }

          const rows = products.map(p => `
            <tr>
              <td style="width:72px"><img src="${p.imageUrl ?? ''}" alt=""/></td>
              <td>${p.name ?? ''}<div style="color:#666;font-size:12px">${p.sku ?? ''}</div></td>
              <td style="width:80px">${p.quantity ?? 0}</td>
              <td style="width:120px">${p.unitPrice ?? ''}</td>
            </tr>
          `).join('');

          root.innerHTML = `
            <table>
              <thead><tr><th></th><th>Product</th><th>Qty</th><th>Unit</th></tr></thead>
              <tbody>${rows}</tbody>
            </table>
            <div class="total">Total: ${total ?? ''}</div>
          `;
        }

        app.ontoolresult = (result) => {
          try{
            render(result);
          }catch(e){
            document.getElementById('root').innerText = 'Failed to render cart';
            console.error(e);
          }
        };

        await app.connect();
      </script>
    </body>
    </html>
    """;
}
