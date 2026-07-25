namespace Nop.Plugin.AI.McpApp.Mcp.Resources;

public static class CatalogWidgetHtml
{
    //public const string Markup = """
    //<!DOCTYPE html>
    //<html>
    //<head>
    //</head>
    //<body>
    //<h1>Hello world</h1>
    //</body>
    //</html>
    //""";

    public const string Markup = """
    <!DOCTYPE html>
    <html>
    <head>
      <style>
    body {
        margin: 0;
        font-family: system-ui, sans-serif;
        padding: 12px;
    }

    .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
        gap: 12px;
    }

    .card {
        border: 1px solid #e2e2e2;
        border-radius: 8px;
        padding: 10px;
        display: flex;
        flex-direction: column;
    }

    .card img {
        width: 100%;
        border-radius: 4px;
    }

    .price {
        font-weight: 600;
        margin-top: 6px;
    }

    .add-btn {
        margin-top: 8px;
        padding: 8px 10px;
        border: 0;
        border-radius: 6px;
        background: #1f6feb;
        color: #fff;
        font-weight: 600;
        cursor: pointer;
    }

    .add-btn:disabled {
        background: #8ab4f8;
        cursor: default;
    }
    </style>
    </head>
    <body>
      <div id="grid" class="grid"></div>
      <script type="module">
        import { App } from "https://esm.sh/@modelcontextprotocol/ext-apps@1.7.1?deps=zod@3.25.76";

        const app = new App({
          name: "catalog-search",
          version: "1.0.0"
        });

        function bindAddToCart() {
            document.querySelectorAll(".add-btn").forEach(btn => {
            btn.addEventListener("click", async (e) => {

                const el = e.currentTarget;
                const productId = Number(el.dataset.productId);
                if (!productId) return;

                const original = el.textContent;
                el.disabled = true;
                el.textContent = "Adding...";

                try {
                    const result = await app.callServerTool({
                        name: "add_to_cart",
                        arguments: {
                            request: { productId }
                        }
                    });

                    el.textContent = result?.isError ? "Failed" : "Added \u2713";
                }
                catch (err) {
                    console.error(err);
                    el.textContent = "Failed";

                    try {
                        await app.sendLog({
                            level: "error",
                            data: {
                                error: String(err)
                            }
                        });
                    }
                    catch {}
                }
                finally {
                    setTimeout(() => {
                        el.disabled = false;
                        el.textContent = original;
                    }, 1500);
                }
            });
        });
    }

        app.ontoolresult = (result) => {
          const products = result.structuredContent?.products ?? [];
          const grid = document.getElementById("grid");
          grid.innerHTML = products.map(p => `
            <div class="card">
              <img src="${p.imageUrl ?? ''}" alt="${p.name}" />
              <div>${p.name}</div>
              <div class="price">${p.price}</div>
              <button class="add-btn" data-product-id="${p.id}">
                 Add to cart
              </button>
            </div>
          `).join("");
            bindAddToCart();
        };

        await app.connect();
      </script>
    </body>
    </html>
    """;
}