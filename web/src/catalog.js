export function catalogHref(slug, term) {
  const params = new URLSearchParams();
  if (term) params.set("q", term);
  if (slug) params.set("c", slug);
  const query = params.toString();
  return query ? `/?${query}` : "/";
}

export function childrenOf(categories, parentId) {
  return categories.filter((category) => (category.parentId ?? null) === (parentId ?? null));
}

export function pathOf(categories, id) {
  const map = Object.fromEntries(categories.map((category) => [category.id, category]));
  const path = [];
  const seen = new Set();
  let current = map[id];
  while (current && !seen.has(current.id)) {
    seen.add(current.id);
    path.unshift(current);
    current = current.parentId ? map[current.parentId] : null;
  }
  return path;
}

export function labeledOptions(categories) {
  return [...categories]
    .map((category) => ({
      ...category,
      label: pathOf(categories, category.id).map((item) => item.name).join(" > ")
    }))
    .sort((a, b) => a.label.localeCompare(b.label, "pt-BR"));
}

export const BRAZIL_STATES = [
  "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
  "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
];

export function cepDigits(value) {
  return String(value ?? "").replace(/\D/g, "").slice(0, 8);
}

export function formatCep(value) {
  const digits = cepDigits(value);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

export const LAST_UNITS = 3;

export function isLastUnits(units) {
  const value = Number(units);
  return value > 0 && value <= LAST_UNITS;
}

export function listingStatus(product) {
  const units = Number(product?.availableUnits ?? 0);
  if (product?.status === "Sold" || units < 1) {
    return { key: "sold", label: "Esgotado" };
  }
  if (product?.status === "Closed") {
    return { key: "closed", label: "Fechado" };
  }
  if (isLastUnits(units)) {
    return { key: "last", label: "Últimas unidades" };
  }
  return { key: "active", label: "Ativo" };
}

export function orderStatusLabel(status) {
  const value = String(status ?? "").toLowerCase();
  if (value === "purchased" || value === "paid") return "Comprado";
  if (value === "shipped") return "Enviado";
  if (value === "delivered") return "Entregue";
  if (value === "cancelled") return "Cancelado";
  return status || "Pedido";
}

export function formatAddress(product) {
  const city = (product?.city || "").trim();
  const state = (product?.state || "").trim();
  const cityAlreadyHasState = Boolean(state) && new RegExp(`(?:^|[\\s,\\-/])${state}$`, "i").test(city);
  const cityState = cityAlreadyHasState ? city : [city, state].filter(Boolean).join(" - ");
  const zip = product?.zipCode ? `CEP ${product.zipCode}` : "";
  return [product?.street, product?.neighborhood, cityState, zip].filter(Boolean).join(" · ");
}
