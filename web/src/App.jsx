import { Link, NavLink, Navigate, Route, Routes, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useEffect, useId, useMemo, useRef, useState } from "react";
import { api, coverUrl, money, parseMoney, uploadImage } from "./api.js";
import { catalogHref, childrenOf, labeledOptions, pathOf, BRAZIL_STATES, formatAddress, formatCep, cepDigits, isLastUnits, listingStatus, orderStatusLabel } from "./catalog.js";
import { useAuth } from "./auth.jsx";

export default function App() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [apiUp, setApiUp] = useState(null);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");

  useEffect(() => {
    fetch("/health")
      .then((response) => setApiUp(response.ok))
      .catch(() => setApiUp(false));
  }, []);

  function onSearch(event) {
    event.preventDefault();
    navigate(catalogHref(searchParams.get("c"), query.trim()));
  }

  return (
    <div className="shell">
      <header className="topbar">
        <div className="topbar-main">
          <Link to="/" className="brand">
            <span className="brand-mark" aria-hidden="true" />
            ECommerce
          </Link>
          <form className="search" onSubmit={onSearch}>
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Buscar anúncios, cidade ou vendedor"
              aria-label="Buscar anúncios"
            />
            <button type="submit">Buscar</button>
          </form>
          <div className="session">
            {auth.user ? (
              <>
                <span className="who">Olá, {auth.user.name.split(" ")[0]}</span>
                <button type="button" className="ghost" onClick={auth.logout}>Sair</button>
              </>
            ) : (
              <>
                <Link to="/login" className="ghost">Entrar</Link>
                <Link to="/register" className="btn">Criar conta</Link>
              </>
            )}
          </div>
        </div>
        <nav className="nav">
          <NavLink to="/" end>Anúncios</NavLink>
          {auth.user && <NavLink to="/listings">Meus anúncios</NavLink>}
          {auth.user && <NavLink to="/sales">Vendas</NavLink>}
          <NavLink to="/sell">Anunciar</NavLink>
          <NavLink to="/cart">Carrinho</NavLink>
          <NavLink to="/orders">Pedidos</NavLink>
        </nav>
      </header>

      {apiUp === false && (
        <div className="banner">
          API indisponível. Suba o backend em <code>localhost:5264</code>.
        </div>
      )}

      <main className="content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/products/:id" element={<ProductPage />} />
          <Route path="/login" element={<Guest><LoginPage /></Guest>} />
          <Route path="/register" element={<Guest><RegisterPage /></Guest>} />
          <Route path="/sell" element={<Private><SellPage /></Private>} />
          <Route path="/listings" element={<Private><ListingsPage /></Private>} />
          <Route path="/cart" element={<Private><CartPage /></Private>} />
          <Route path="/orders" element={<Private><OrdersPage /></Private>} />
          <Route path="/sales" element={<Private><SalesPage /></Private>} />
        </Routes>
      </main>

      <footer className="footer">
        Marketplace acadêmico · compre e venda com a mesma conta
      </footer>
    </div>
  );
}

function Private({ children }) {
  const auth = useAuth();
  if (!auth.isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return children;
}

function Guest({ children }) {
  const auth = useAuth();
  if (auth.isAuthenticated) {
    return <Navigate to="/" replace />;
  }
  return children;
}

function PasswordField({ value, onChange, autoComplete, minLength, required = true, id }) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const [visible, setVisible] = useState(false);
  return (
    <div className="password-field">
      <input
        id={inputId}
        value={value}
        onChange={onChange}
        type={visible ? "text" : "password"}
        autoComplete={autoComplete}
        minLength={minLength}
        required={required}
      />
      <button
        type="button"
        className="password-toggle"
        onClick={() => setVisible((current) => !current)}
        aria-label={visible ? "Ocultar senha" : "Mostrar senha"}
      >
        {visible ? "Ocultar" : "Mostrar"}
      </button>
    </div>
  );
}

function AuthPanel({ title, text, points }) {
  return (
    <aside className="auth-panel">
      <p className="eyebrow">Marketplace</p>
      <h1>{title}</h1>
      <p>{text}</p>
      <ul className="auth-points">
        {points.map((point) => (
          <li key={point}>{point}</li>
        ))}
      </ul>
    </aside>
  );
}

function AuthTabs({ current }) {
  return (
    <div className="auth-tabs" role="tablist" aria-label="Acesso">
      <Link to="/login" role="tab" aria-selected={current === "login"} className={current === "login" ? "active" : ""}>
        Entrar
      </Link>
      <Link to="/register" role="tab" aria-selected={current === "register"} className={current === "register" ? "active" : ""}>
        Criar conta
      </Link>
    </div>
  );
}

function HomePage() {
  const [searchParams] = useSearchParams();
  const term = (searchParams.get("q") ?? "").trim().toLowerCase();
  const selected = searchParams.get("c") ?? "";
  const [data, setData] = useState(null);
  const [categories, setCategories] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    const url = selected ? `/api/products?category=${encodeURIComponent(selected)}` : "/api/products";
    Promise.all([api(url), api("/api/categories")])
      .then(([products, catalog]) => {
        setData(products);
        setCategories(catalog.items ?? []);
      })
      .catch((err) => setError(err.message));
  }, [selected]);

  const items = useMemo(() => {
    const list = data?.items ?? [];
    if (!term) return list;
    return list.filter((product) =>
      [product.title, product.description, product.city, product.sellerName, product.categoryName]
        .join(" ")
        .toLowerCase()
        .includes(term)
    );
  }, [data, term]);

  const current = categories.find((category) => category.slug === selected || category.id === selected);
  const crumbs = current ? pathOf(categories, current.id) : [];

  if (error) return <Notice title="Não foi possível carregar os anúncios">{error}</Notice>;
  if (!data) return <div className="skeleton" aria-hidden="true" />;

  const heading = current?.name
    ? current.name
    : term
      ? `Resultados para “${term}”`
      : "Encontre o que você precisa";

  return (
    <div className="catalog">
      <aside className="filters">
        <p className="filters-title">Categorias</p>
        <div className="filters-scroll">
          <Link className={`filter-link ${selected ? "" : "active"}`} to={catalogHref("", term)}>
            Todas
          </Link>
          <CategoryTree
            categories={categories}
            parentId={null}
            selected={selected}
            term={term}
          />
        </div>
      </aside>
      <section>
        <nav className="crumbs" aria-label="Caminho">
          <Link to={catalogHref("", term)}>Início</Link>
          {crumbs.map((step) => (
            <span key={step.id}>
              <span className="crumbs-sep" aria-hidden="true">›</span>
              <Link to={catalogHref(step.slug || step.id, term)}>{step.name}</Link>
            </span>
          ))}
        </nav>
        <div className="hero">
          <div>
            <p className="eyebrow">Marketplace</p>
            <h1>{heading}</h1>
            <p className="muted">{items.length} anúncio(s) {term || selected ? "nesta seleção" : "publicados"}</p>
          </div>
          <Link to="/sell" className="btn">Vender agora</Link>
        </div>
        {items.length === 0 ? (
          <Notice title={term || selected ? "Nada encontrado" : "Nenhum anúncio ainda"}>
            {term || selected
              ? "Tente outra categoria ou limpe a busca."
              : "Crie uma conta e publique o primeiro produto para testar o catálogo."}
          </Notice>
        ) : (
          <div className="grid">
            {items.map((product) => (
              <Link key={product.id} to={`/products/${product.id}`} className="card">
                <div className="thumb">
                  <img src={coverUrl(product)} alt="" />
                  <span className="badge">{product.condition === "New" ? "Novo" : "Usado"}</span>
                  {isLastUnits(product.availableUnits) && (
                    <span className="badge low">Últimas unidades</span>
                  )}
                </div>
                <h2>{product.title}</h2>
                <p className="price">{money(product.price)}</p>
                {product.description && (
                  <>
                    <p className="card-desc">{product.description}</p>
                    {needsMore(product.description) && <span className="card-more">Ver mais</span>}
                  </>
                )}
                <p className="muted">
                  {(product.categoryPath ?? []).map((step) => step.name).join(" › ") || "Sem categoria"}
                </p>
                <p className="muted">
                  {product.city || "Brasil"} · {product.sellerName}
                  {isLastUnits(product.availableUnits)
                    ? ` · Últimas unidades (${product.availableUnits})`
                    : ` · ${product.availableUnits} un.`}
                </p>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function CategoryTree({ categories, parentId, selected, term }) {
  const nodes = childrenOf(categories, parentId);
  if (!nodes.length) return null;
  return (
    <ul className="filter-tree">
      {nodes.map((category) => {
        const active = selected === category.slug || selected === category.id;
        return (
          <li key={category.id}>
            <Link
              className={`filter-link ${active ? "active" : ""}`}
              to={catalogHref(category.slug || category.id, term)}
            >
              {category.name}
            </Link>
            <CategoryTree
              categories={categories}
              parentId={category.id}
              selected={selected}
              term={term}
            />
          </li>
        );
      })}
    </ul>
  );
}

function needsMore(text) {
  const value = String(text ?? "").trim();
  if (!value) return false;
  return value.length > 100 || value.split(/\n/).filter(Boolean).length > 2;
}

function ExpandableText({ text }) {
  const [open, setOpen] = useState(false);
  const long = needsMore(text);

  useEffect(() => {
    setOpen(false);
  }, [text]);

  if (!text) return null;

  return (
    <div className="lead-block">
      <p className={`lead-short ${open || !long ? "is-open" : ""}`}>{text}</p>
      {long && (
        <button type="button" className="more-link" onClick={() => setOpen((current) => !current)}>
          {open ? "Ver menos" : "Ver mais"}
        </button>
      )}
    </div>
  );
}

function ProductPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const auth = useAuth();
  const [product, setProduct] = useState(null);
  const [photo, setPhoto] = useState(0);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    api(`/api/products/${id}`)
      .then((data) => {
        setProduct(data);
        setPhoto(0);
      })
      .catch((err) => setError(err.message));
  }, [id]);

  async function addToCart() {
    if (!auth.isAuthenticated) {
      navigate("/login");
      return;
    }
    setStatus("");
    try {
      await api("/api/cart/items", {
        method: "POST",
        body: JSON.stringify({ productId: product.id, quantity: 1 })
      });
      setStatus("Adicionado ao carrinho.");
    } catch (err) {
      setStatus(err.message);
    }
  }

  if (error) return <Notice title="Anúncio não encontrado">{error}</Notice>;
  if (!product) return <div className="skeleton" aria-hidden="true" />;

  const photos = (product.images ?? []).filter(Boolean);
  const currentPhoto = photos[photo] || coverUrl(product);
  const closed = product.status === "Closed";
  const soldOut = product.status === "Sold" || (product.availableUnits ?? 0) < 1;
  const lastUnits = isLastUnits(product.availableUnits);
  const ownListing = auth.user?.id === product.sellerId;
  const canBuy = !closed && !soldOut && !ownListing;

  return (
    <article className="product-page">
      <div className="detail">
        <div>
          <div className="thumb large">
            <img src={currentPhoto} alt={product.title} />
            <span className="badge">{product.condition === "New" ? "Novo" : "Usado"}</span>
            {lastUnits && <span className="badge low">Últimas unidades</span>}
            {soldOut && <span className="badge sold">Esgotado</span>}
          </div>
          {photos.length > 1 && (
            <div className="gallery">
              {photos.map((src, index) => (
                <button
                  key={src}
                  type="button"
                  className={index === photo ? "active" : ""}
                  onClick={() => setPhoto(index)}
                >
                  <img src={src} alt="" />
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="buybox">
          {product.categoryPath?.length > 0 && (
            <nav className="crumbs" aria-label="Categoria">
              <Link to="/">Início</Link>
              {product.categoryPath.map((step) => (
                <span key={step.id}>
                  <span className="crumbs-sep" aria-hidden="true">›</span>
                  <Link to={catalogHref(step.slug || step.id)}>{step.name}</Link>
                </span>
              ))}
            </nav>
          )}
          <p className="muted">{product.city || "Brasil"} · {product.sellerName}</p>
          <h1>{product.title}</h1>
          {product.description && <ExpandableText text={product.description} />}
          <p className="price">{money(product.price)}</p>
          {closed ? (
            <p className="error">Este anúncio está fechado.</p>
          ) : soldOut ? (
            <p className="error">Este anúncio saiu da vitrine: sem unidades disponíveis.</p>
          ) : (
            <p className={`stock ${lastUnits ? "last" : ""}`}>
              {lastUnits
                ? `Últimas unidades · ${product.availableUnits} disponível(is)`
                : `${product.availableUnits} unidade(s) disponível(is)`}
            </p>
          )}
          <button type="button" className="btn wide" onClick={addToCart} disabled={!canBuy}>
            {ownListing
              ? "Seu anúncio"
              : closed
                ? "Anúncio fechado"
                : soldOut
                  ? "Esgotado"
                  : "Adicionar ao carrinho"}
          </button>
          {status && <p className="ok">{status}</p>}
        </div>
      </div>
      <section className="description">
        <h2>Informações do anúncio</h2>
        <dl className="facts">
          <div>
            <dt>Vendedor</dt>
            <dd>{product.sellerName}</dd>
          </div>
          <div>
            <dt>Condição</dt>
            <dd>{product.condition === "New" ? "Novo" : "Usado"}</dd>
          </div>
          <div>
            <dt>Categoria</dt>
            <dd>{(product.categoryPath ?? []).map((step) => step.name).join(" › ") || "—"}</dd>
          </div>
          <div>
            <dt>Endereço</dt>
            <dd>{formatAddress(product) || product.city || "Não informado"}</dd>
          </div>
          <div>
            <dt>Unidades</dt>
            <dd>
              {soldOut
                ? "Esgotado"
                : lastUnits
                  ? `Últimas unidades (${product.availableUnits})`
                  : product.availableUnits ?? 0}
            </dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>{listingStatus(product).label}</dd>
          </div>
          <div>
            <dt>Publicado</dt>
            <dd>{new Date(product.createdAt).toLocaleString("pt-BR")}</dd>
          </div>
        </dl>
      </section>
    </article>
  );
}

function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: "", password: "" });
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  async function onSubmit(event) {
    event.preventDefault();
    setError("");
    setSaving(true);
    try {
      await auth.login(form);
      navigate("/");
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="auth">
      <AuthPanel
        title="Entre para comprar e vender"
        text="Uma conta, dois papéis. Anúncios, carrinho e pedidos no mesmo lugar."
        points={["Acompanhe pedidos comprados", "Publique e gerencie anúncios", "Veja quem comprou os seus itens"]}
      />
      <form className="form auth-card" onSubmit={onSubmit}>
        <AuthTabs current="login" />
        <div className="auth-copy">
          <h2>Bem-vindo de volta</h2>
          <p className="muted">Use o e-mail e a senha da sua conta.</p>
        </div>
        <label>E-mail
          <input
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            type="email"
            autoComplete="email"
            placeholder="voce@email.com"
            required
          />
        </label>
        <div className="field-block">
          <label htmlFor="login-password">Senha</label>
          <PasswordField
            id="login-password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="current-password"
          />
        </div>
        {error && <p className="error" role="alert">{error}</p>}
        <button className="btn wide" type="submit" disabled={saving}>
          {saving ? "Entrando..." : "Entrar"}
        </button>
        <p className="auth-switch muted">Novo por aqui? <Link to="/register">Criar conta grátis</Link></p>
      </form>
    </div>
  );
}

function RegisterPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ name: "", email: "", password: "", confirm: "", asSeller: true });
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  async function onSubmit(event) {
    event.preventDefault();
    setError("");
    if (form.password !== form.confirm) {
      setError("As senhas não coincidem.");
      return;
    }
    setSaving(true);
    try {
      await auth.register({
        name: form.name,
        email: form.email,
        password: form.password,
        asSeller: form.asSeller
      });
      navigate("/");
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="auth">
      <AuthPanel
        title="Crie sua conta em poucos segundos"
        text="Cadastre-se uma vez e use o marketplace para comprar e vender."
        points={["Sem taxa para publicar", "Mesma conta para os dois papéis", "Pedidos e vendas em um só painel"]}
      />
      <form className="form auth-card" onSubmit={onSubmit}>
        <AuthTabs current="register" />
        <div className="auth-copy">
          <h2>Criar conta</h2>
          <p className="muted">Leva menos de um minuto para começar.</p>
        </div>
        <label>Nome
          <input
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            autoComplete="name"
            placeholder="Seu nome"
            required
          />
        </label>
        <label>E-mail
          <input
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            type="email"
            autoComplete="email"
            placeholder="voce@email.com"
            required
          />
        </label>
        <div className="field-block">
          <label htmlFor="register-password">Senha</label>
          <PasswordField
            id="register-password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="new-password"
            minLength={8}
          />
          <span className="field-hint">Mínimo de 8 caracteres.</span>
        </div>
        <div className="field-block">
          <label htmlFor="register-confirm">Confirmar senha</label>
          <PasswordField
            id="register-confirm"
            value={form.confirm}
            onChange={(e) => setForm({ ...form, confirm: e.target.value })}
            autoComplete="new-password"
            minLength={8}
          />
        </div>
        <fieldset className="role-picker">
          <legend>Como você vai usar a conta?</legend>
          <div className="role-cards">
            <button
              type="button"
              className={form.asSeller ? "active" : ""}
              onClick={() => setForm({ ...form, asSeller: true })}
            >
              <strong>Comprar e vender</strong>
              <span>Publique anúncios e acompanhe vendas</span>
            </button>
            <button
              type="button"
              className={!form.asSeller ? "active" : ""}
              onClick={() => setForm({ ...form, asSeller: false })}
            >
              <strong>Só comprar</strong>
              <span>Carrinho, pedidos e status Comprado</span>
            </button>
          </div>
        </fieldset>
        {error && <p className="error" role="alert">{error}</p>}
        <button className="btn wide" type="submit" disabled={saving}>
          {saving ? "Criando conta..." : "Criar conta"}
        </button>
        <p className="auth-switch muted">Já tem conta? <Link to="/login">Entrar</Link></p>
      </form>
    </div>
  );
}

function SellPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    title: "",
    description: "",
    price: "",
    city: "",
    state: "SP",
    neighborhood: "",
    street: "",
    zipCode: "",
    availableUnits: 1,
    condition: "Used",
    categoryId: ""
  });
  const [categories, setCategories] = useState([]);
  const [files, setFiles] = useState([]);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [cepStatus, setCepStatus] = useState("");
  const lastCep = useRef("");
  const [newCategory, setNewCategory] = useState({ name: "", parentId: "" });

  useEffect(() => {
    api("/api/categories")
      .then((data) => setCategories(data.items ?? []))
      .catch((err) => setError(err.message));
  }, []);

  async function lookupCep(value) {
    const digits = cepDigits(value);
    if (digits.length !== 8 || lastCep.current === digits) {
      return;
    }
    lastCep.current = digits;
    setCepStatus("loading");
    try {
      const data = await api(`/api/cep/${digits}`);
      setForm((current) => ({
        ...current,
        zipCode: formatCep(data.zipCode || digits),
        street: data.street || "",
        neighborhood: data.neighborhood || "",
        city: data.city || current.city,
        state: data.state || current.state
      }));
      setCepStatus("ok");
    } catch (err) {
      lastCep.current = "";
      setCepStatus(err.message || "error");
    }
  }

  function onCepChange(value) {
    const formatted = formatCep(value);
    setForm((current) => ({ ...current, zipCode: formatted }));
    if (cepDigits(formatted).length < 8) {
      lastCep.current = "";
      setCepStatus("");
      return;
    }
    lookupCep(formatted);
  }

  async function loadCategories() {
    const data = await api("/api/categories");
    setCategories(data.items ?? []);
  }

  async function createCategory() {
    setError("");
    try {
      const created = await api("/api/categories", {
        method: "POST",
        body: JSON.stringify({
          name: newCategory.name,
          parentId: newCategory.parentId || null
        })
      });
      await loadCategories();
      setForm((current) => ({ ...current, categoryId: created.id }));
      setNewCategory({ name: "", parentId: newCategory.parentId });
    } catch (err) {
      setError(err.message);
    }
  }

  const previews = files.map((file) => ({
    name: file.name,
    url: URL.createObjectURL(file)
  }));
  const options = labeledOptions(categories);
  const parsedPrice = parseMoney(form.price);

  async function onSubmit(event) {
    event.preventDefault();
    setError("");
    if (!Number.isFinite(parsedPrice) || parsedPrice <= 0) {
      setError("Informe um preço válido, como 2.000,00.");
      return;
    }
    setSaving(true);
    try {
      const images = [];
      for (const file of files.slice(0, 4)) {
        images.push(await uploadImage(file));
      }
      const product = await api("/api/products", {
        method: "POST",
        body: JSON.stringify({
          ...form,
          price: parsedPrice,
          availableUnits: Number(form.availableUnits) || 1,
          images
        })
      });
      navigate(`/products/${product.id}`);
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="form form-wide" onSubmit={onSubmit}>
      <h1>Publicar anúncio</h1>
      <p className="muted">Título, preço, cidade e uma foto já bastam para aparecer na vitrine.</p>
      <label>Título<input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required /></label>
      <label>Descrição
        <textarea
          value={form.description}
          onChange={(e) => setForm({ ...form, description: e.target.value })}
          required
          rows={6}
          placeholder="Conte o estado, o que acompanha e por que está vendendo."
        />
      </label>
      <div className="split">
        <label>Preço
          <input
            value={form.price}
            onChange={(e) => setForm({ ...form, price: e.target.value })}
            inputMode="decimal"
            placeholder="2.000,00"
            required
          />
          <span className="field-hint">
            {Number.isFinite(parsedPrice) && parsedPrice > 0 ? money(parsedPrice) : "Use o formato 2.000,00"}
          </span>
        </label>
        <label>Unidades disponíveis
          <input
            type="number"
            min="1"
            max="9999"
            value={form.availableUnits}
            onChange={(e) => setForm({ ...form, availableUnits: e.target.value })}
            required
          />
        </label>
      </div>
      <div className="split">
        <label>CEP
          <input
            value={form.zipCode}
            onChange={(e) => onCepChange(e.target.value)}
            onBlur={(e) => lookupCep(e.target.value)}
            inputMode="numeric"
            autoComplete="postal-code"
            placeholder="14800-000"
            maxLength={9}
          />
          {cepStatus === "loading" && <span className="field-hint">Buscando endereço...</span>}
          {cepStatus === "ok" && <span className="field-hint">Endereço preenchido pelo CEP.</span>}
          {cepStatus && cepStatus !== "loading" && cepStatus !== "ok" && (
            <span className="field-hint error">{cepStatus}</span>
          )}
        </label>
        <label>Bairro<input value={form.neighborhood} onChange={(e) => setForm({ ...form, neighborhood: e.target.value })} placeholder="Centro" /></label>
      </div>
      <div className="split">
        <label>Cidade<input value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} required /></label>
        <label>Estado
          <select value={form.state} onChange={(e) => setForm({ ...form, state: e.target.value })} required>
            {BRAZIL_STATES.map((uf) => (
              <option key={uf} value={uf}>{uf}</option>
            ))}
          </select>
        </label>
      </div>
      <label>Endereço
        <input
          value={form.street}
          onChange={(e) => setForm({ ...form, street: e.target.value })}
          placeholder="Número e complemento (a rua vem do CEP)"
        />
      </label>
      <label>Categoria
        <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })} required>
          <option value="">Selecione</option>
          {options.map((category) => (
            <option key={category.id} value={category.id}>{category.label}</option>
          ))}
        </select>
      </label>
      <div className="category-create">
        <p className="filters-title">Nova categoria</p>
        <div className="split">
          <label>Nome
            <input
              value={newCategory.name}
              onChange={(e) => setNewCategory({ ...newCategory, name: e.target.value })}
              placeholder="Ex.: Placa de vídeo"
            />
          </label>
          <label>Dentro de
            <select
              value={newCategory.parentId}
              onChange={(e) => setNewCategory({ ...newCategory, parentId: e.target.value })}
            >
              <option value="">Categoria raiz</option>
              {options.map((category) => (
                <option key={category.id} value={category.id}>{category.label}</option>
              ))}
            </select>
          </label>
        </div>
        <button type="button" className="ghost" onClick={createCategory} disabled={!newCategory.name.trim()}>
          Adicionar categoria
        </button>
      </div>
      <label>Condição
        <select value={form.condition} onChange={(e) => setForm({ ...form, condition: e.target.value })}>
          <option value="Used">Usado</option>
          <option value="New">Novo</option>
        </select>
      </label>
      <label>Fotos (até 4, 2 MB cada)
        <input
          type="file"
          accept="image/jpeg,image/png,image/webp"
          multiple
          onChange={(event) => setFiles([...event.target.files].slice(0, 4))}
        />
      </label>
      {previews.length > 0 && (
        <div className="previews">
          {previews.map((preview) => (
            <img key={preview.name} src={preview.url} alt="" />
          ))}
        </div>
      )}
      {error && <p className="error">{error}</p>}
      <button className="btn" type="submit" disabled={saving}>{saving ? "Publicando..." : "Publicar"}</button>
    </form>
  );
}

function ListingsPage() {
  const [data, setData] = useState(null);
  const [units, setUnits] = useState({});
  const [error, setError] = useState("");
  const [busy, setBusy] = useState("");

  useEffect(() => {
    api("/api/products/mine")
      .then((result) => {
        setData(result);
        setUnits(Object.fromEntries((result.items ?? []).map((item) => [item.id, item.availableUnits])));
      })
      .catch((err) => setError(err.message));
  }, []);

  async function patch(id, body) {
    setBusy(id);
    setError("");
    try {
      const updated = await api(`/api/products/${id}`, {
        method: "PATCH",
        body: JSON.stringify(body)
      });
      setData((current) => ({
        ...current,
        items: (current.items ?? []).map((item) => (item.id === id ? updated : item))
      }));
      setUnits((current) => ({ ...current, [id]: updated.availableUnits }));
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy("");
    }
  }

  if (error && !data) return <Notice title="Não foi possível carregar seus anúncios">{error}</Notice>;
  if (!data) return <div className="skeleton" aria-hidden="true" />;

  const items = data.items ?? [];

  return (
    <section>
      <div className="page-head">
        <div>
          <p className="eyebrow">Vendedor</p>
          <h1>Meus anúncios</h1>
          <p className="muted">{data.total ?? items.length} anúncio(s) publicados</p>
        </div>
        <Link to="/sell" className="btn">Novo anúncio</Link>
      </div>
      {error && <p className="error">{error}</p>}
      {items.length === 0 ? (
        <Notice title="Você ainda não anunciou">
          Publique um produto para gerenciar unidades e fechar o anúncio por aqui.
        </Notice>
      ) : (
        <ul className="list">
          {items.map((product) => {
            const closed = product.status === "Closed";
            const state = listingStatus(product);
            return (
              <li key={product.id} className="listing-row">
                <div className="listing-main">
                  <Link to={`/products/${product.id}`} className="listing-thumb">
                    <img src={coverUrl(product)} alt="" />
                  </Link>
                  <div className="listing-copy">
                    <Link to={`/products/${product.id}`}>{product.title}</Link>
                    <p className="muted">{money(product.price)} · {product.city || "Brasil"}</p>
                    <div className="listing-meta">
                      <span className={`status ${state.key}`}>{state.label}</span>
                      <span className="muted">{product.availableUnits} un.</span>
                    </div>
                  </div>
                </div>
                <div className="listing-actions">
                  <label>
                    Unidades
                    <input
                      type="number"
                      min="0"
                      max="9999"
                      value={units[product.id] ?? product.availableUnits}
                      onChange={(event) => setUnits((current) => ({ ...current, [product.id]: event.target.value }))}
                      disabled={busy === product.id}
                    />
                  </label>
                  <button
                    type="button"
                    className="ghost"
                    disabled={busy === product.id}
                    onClick={() => patch(product.id, { availableUnits: Number(units[product.id]) })}
                  >
                    Salvar
                  </button>
                  <button
                    type="button"
                    className={closed ? "btn" : "ghost"}
                    disabled={busy === product.id}
                    onClick={() => patch(product.id, { status: closed ? "Active" : "Closed" })}
                  >
                    {closed ? "Reabrir" : "Fechar"}
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

function CartPage() {
  const navigate = useNavigate();
  const [cart, setCart] = useState(null);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");
  const [checkingOut, setCheckingOut] = useState(false);

  useEffect(() => {
    api("/api/cart")
      .then(async (data) => {
        const items = await Promise.all((data.items ?? []).map(async (item) => {
          try {
            const product = await api(`/api/products/${item.productId}`);
            return { ...item, product };
          } catch {
            return { ...item, product: null };
          }
        }));
        setCart({ ...data, items });
      })
      .catch((err) => setError(err.message));
  }, []);

  async function checkout() {
    setError("");
    setStatus("");
    setCheckingOut(true);
    try {
      await api("/api/orders", { method: "POST" });
      navigate("/orders");
    } catch (err) {
      setError(err.message);
    } finally {
      setCheckingOut(false);
    }
  }

  if (error && !cart) return <Notice title="Carrinho indisponível">{error}</Notice>;
  if (!cart) return <div className="skeleton" aria-hidden="true" />;

  const items = cart.items ?? [];
  const total = items.reduce((sum, item) => sum + Number(item.product?.price ?? 0) * Number(item.quantity ?? 0), 0);

  return (
    <section>
      <div className="page-head">
        <h1>Carrinho</h1>
        <Link to="/" className="ghost">Continuar comprando</Link>
      </div>
      {error && <p className="error">{error}</p>}
      {status && <p className="ok">{status}</p>}
      {items.length ? (
        <>
          <ul className="list">
            {items.map((item) => (
              <li key={item.productId}>
                {item.product && <img className="cart-thumb" src={coverUrl(item.product)} alt="" />}
                <div>
                  <strong>{item.product?.title ?? "Anúncio"}</strong>
                  <p className="muted">
                    {item.product ? money(item.product.price) : item.productId}
                    {isLastUnits(item.product?.availableUnits) ? " · Últimas unidades" : ""}
                  </p>
                </div>
                <span className="qty">Qtd. {item.quantity}</span>
              </li>
            ))}
          </ul>
          <div className="cart-summary">
            <p className="price">{money(total)}</p>
            <button type="button" className="btn" onClick={checkout} disabled={checkingOut}>
              {checkingOut ? "Finalizando..." : "Finalizar compra"}
            </button>
          </div>
        </>
      ) : (
        <Notice title="Carrinho vazio">Abra um anúncio e clique em adicionar ao carrinho.</Notice>
      )}
    </section>
  );
}

function OrdersPage() {
  const [orders, setOrders] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api("/api/orders")
      .then(setOrders)
      .catch((err) => setError(err.message));
  }, []);

  if (error) return <Notice title="Pedidos indisponíveis">{error}</Notice>;
  if (!orders) return <div className="skeleton" aria-hidden="true" />;

  return (
    <section>
      <div className="page-head">
        <div>
          <p className="eyebrow">Comprador</p>
          <h1>Meus pedidos</h1>
        </div>
      </div>
      {orders.length ? (
        <ul className="list">
          {orders.map((order) => (
            <OrderCard key={order.id} order={order} perspective="buyer" />
          ))}
        </ul>
      ) : (
        <Notice title="Nenhum pedido">Adicione itens ao carrinho e clique em Finalizar compra.</Notice>
      )}
    </section>
  );
}

function SalesPage() {
  const [orders, setOrders] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api("/api/orders/sales")
      .then(setOrders)
      .catch((err) => setError(err.message));
  }, []);

  if (error) return <Notice title="Vendas indisponíveis">{error}</Notice>;
  if (!orders) return <div className="skeleton" aria-hidden="true" />;

  return (
    <section>
      <div className="page-head">
        <div>
          <p className="eyebrow">Vendedor</p>
          <h1>Vendas</h1>
          <p className="muted">Quem comprou os seus anúncios</p>
        </div>
      </div>
      {orders.length ? (
        <ul className="list">
          {orders.map((order) => (
            <OrderCard key={order.id} order={order} perspective="seller" />
          ))}
        </ul>
      ) : (
        <Notice title="Nenhuma venda ainda">Quando alguém finalizar a compra de um anúncio seu, o comprador aparece aqui.</Notice>
      )}
    </section>
  );
}

function OrderCard({ order, perspective }) {
  const items = order.items ?? [];
  return (
    <li className="order-card">
      <div className="order-head">
        <div>
          <strong>Pedido {order.id.slice(0, 8)}</strong>
          <p className="muted">{new Date(order.createdAt).toLocaleString("pt-BR")}</p>
        </div>
        <span className="status purchased">{orderStatusLabel(order.status)}</span>
        <strong>{money(order.total)}</strong>
      </div>
      <p className="muted">
        {perspective === "seller"
          ? `Comprador: ${order.buyerName || "Cliente"} · ${order.buyerEmail || "sem e-mail"}`
          : `Vendedor: ${order.sellerName || "Anunciante"}`}
      </p>
      {items.length > 0 && (
        <ul className="order-items">
          {items.map((item) => (
            <li key={`${order.id}-${item.productId}`}>
              <div className="order-thumb-wrap">
                <img
                  className="order-thumb"
                  src={coverUrl({ id: item.productId, image: item.image })}
                  alt=""
                />
                <span className="order-qty">{item.quantity} un.</span>
              </div>
              <div className="order-item-copy">
                <div>
                  <span>{item.title}</span>
                  <p className="order-qty-text">
                    {item.quantity} {item.quantity === 1 ? "unidade" : "unidades"}
                  </p>
                </div>
                <div className="order-item-price">
                  <span className="muted">{item.quantity} × {money(item.unitPrice)}</span>
                  <strong>{money(Number(item.unitPrice) * Number(item.quantity))}</strong>
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </li>
  );
}

function Notice({ title, children }) {
  return (
    <div className="notice">
      <strong>{title}</strong>
      <p>{children}</p>
    </div>
  );
}
