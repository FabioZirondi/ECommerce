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

export function paymentMethodLabel(method) {
  const value = String(method ?? "").toLowerCase();
  if (value === "pix") return "PIX";
  if (value === "card" || value === "cartao" || value === "cartão") return "Cartão";
  if (value === "boleto") return "Boleto";
  return "";
}

function emv(id, value) {
  return `${id}${String(value.length).padStart(2, "0")}${value}`;
}

function crc16(payload) {
  let crc = 0xffff;
  for (let index = 0; index < payload.length; index += 1) {
    crc ^= payload.charCodeAt(index) << 8;
    for (let bit = 0; bit < 8; bit += 1) {
      crc = crc & 0x8000 ? (crc << 1) ^ 0x1021 : crc << 1;
      crc &= 0xffff;
    }
  }
  return crc.toString(16).toUpperCase().padStart(4, "0");
}

export function buildPixCopiaECola(amount, txid) {
  const merchant = emv("00", "BR.GOV.BCB.PIX") + emv("01", "inovatech@pix");
  const additional = emv("05", String(txid || "INOVATECH").replace(/[^A-Za-z0-9]/g, "").slice(0, 25) || "INOVATECH");
  const body = [
    emv("00", "01"),
    emv("26", merchant),
    emv("52", "0000"),
    emv("53", "986"),
    emv("54", Number(amount || 0).toFixed(2)),
    emv("58", "BR"),
    emv("59", "INOVATECH"),
    emv("60", "SAO PAULO"),
    emv("62", additional),
    "6304"
  ].join("");
  return body + crc16(body);
}

function foldPlace(value) {
  return String(value ?? "")
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .trim();
}

function addBusinessDays(start, days) {
  const date = new Date(start);
  date.setHours(12, 0, 0, 0);
  let added = 0;
  while (added < days) {
    date.setDate(date.getDate() + 1);
    const weekday = date.getDay();
    if (weekday !== 0 && weekday !== 6) {
      added += 1;
    }
  }
  return date;
}

export function hasDeliveryCep(user) {
  return cepDigits(user?.zipCode).length === 8;
}

export function estimateDelivery(origin, dest) {
  const destCity = foldPlace(dest?.city);
  const destState = foldPlace(dest?.state);
  const destZip = cepDigits(dest?.zipCode);
  if (!destCity && destZip.length !== 8) {
    return null;
  }

  const originCity = foldPlace(origin?.city);
  const originState = foldPlace(origin?.state);
  const originZip = cepDigits(origin?.zipCode);
  if (!originCity && !originState && originZip.length !== 8) {
    return null;
  }

  let minDays = 8;
  let maxDays = 15;
  if (originCity && destCity && originCity === destCity
    && (!originState || !destState || originState === destState)) {
    minDays = 2;
    maxDays = 4;
  } else if (originState && destState && originState === destState) {
    minDays = 4;
    maxDays = 7;
  } else if (originZip.length === 8 && destZip.length === 8 && originZip[0] === destZip[0]) {
    minDays = 6;
    maxDays = 10;
  }

  const from = addBusinessDays(new Date(), minDays);
  const to = addBusinessDays(new Date(), maxDays);
  const fmt = (date) => date.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" });
  return {
    minDays,
    maxDays,
    label: `Chega entre ${fmt(from)} e ${fmt(to)}`,
    shortLabel: `${minDays}–${maxDays} dias úteis`
  };
}

export function formatAddress(product) {
  const city = (product?.city || "").trim();
  const state = (product?.state || "").trim();
  const cityAlreadyHasState = Boolean(state) && new RegExp(`(?:^|[\\s,\\-/])${state}$`, "i").test(city);
  const cityState = cityAlreadyHasState ? city : [city, state].filter(Boolean).join(" - ");
  const zip = product?.zipCode ? `CEP ${product.zipCode}` : "";
  return [product?.street, product?.neighborhood, cityState, zip].filter(Boolean).join(" · ");
}
