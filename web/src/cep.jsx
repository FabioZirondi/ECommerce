import { useEffect, useRef, useState } from "react";
import { api } from "./api.js";
import { cepDigits, estimateDelivery, formatCep, hasDeliveryCep } from "./catalog.js";
import { getNearPlace, resolveDeliveryDest, setNearPlace } from "./search.js";
import { useAuth } from "./auth.jsx";

export function DeliveryCepField({
  id = "cep-entrega",
  value,
  onChange,
  disabled,
  hint = "Usamos o CEP para estimar o prazo de chegada."
}) {
  const last = useRef("");
  const [status, setStatus] = useState("");
  const [place, setPlace] = useState("");

  async function lookup(raw) {
    const digits = cepDigits(raw);
    if (digits.length !== 8 || last.current === digits) {
      return;
    }
    last.current = digits;
    setStatus("loading");
    try {
      const data = await api(`/api/cep/${digits}`);
      onChange(formatCep(data.zipCode || digits));
      setPlace([data.city, data.state].filter(Boolean).join(" - "));
      setStatus("ok");
    } catch (err) {
      last.current = "";
      setPlace("");
      setStatus(err.message || "CEP não encontrado.");
    }
  }

  function handleChange(raw) {
    const formatted = formatCep(raw);
    onChange(formatted);
    if (cepDigits(formatted).length < 8) {
      last.current = "";
      setStatus("");
      setPlace("");
      return;
    }
    lookup(formatted);
  }

  return (
    <label htmlFor={id}>CEP de entrega
      <input
        id={id}
        value={value}
        onChange={(event) => handleChange(event.target.value)}
        onBlur={(event) => lookup(event.target.value)}
        inputMode="numeric"
        autoComplete="postal-code"
        placeholder="14800-000"
        maxLength={9}
        required
        disabled={disabled}
      />
      {status === "loading" && <span className="field-hint">Buscando CEP...</span>}
      {status === "ok" && (
        <span className="field-hint">{place ? `Entrega em ${place}.` : "CEP encontrado."}</span>
      )}
      {status && status !== "loading" && status !== "ok" && (
        <span className="field-hint error">{status}</span>
      )}
      {!status && <span className="field-hint">{hint}</span>}
    </label>
  );
}

export function DeliveryEta({ product }) {
  const auth = useAuth();
  const [open, setOpen] = useState(false);
  const [cep, setCep] = useState(() => formatCep(resolveDeliveryDest(auth.user)?.zipCode || ""));
  const [dest, setDest] = useState(() => resolveDeliveryDest(auth.user));
  const [hint, setHint] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const next = resolveDeliveryDest(auth.user) || resolveDeliveryDest(getNearPlace());
    if (!next) {
      return;
    }
    setDest(next);
    if (next.zipCode) {
      setCep(formatCep(next.zipCode));
    }
  }, [auth.user]);

  const eta = dest ? estimateDelivery(product, dest) : null;

  async function onCalculate(event) {
    event.preventDefault();
    const digits = cepDigits(cep);
    if (digits.length !== 8) {
      setError("Informe um CEP com 8 dígitos.");
      return;
    }
    setError("");
    setBusy(true);
    try {
      let next = dest && cepDigits(dest.zipCode) === digits ? dest : null;
      if (!next?.city) {
        const data = await api(`/api/cep/${digits}`);
        next = {
          zipCode: data.zipCode || formatCep(digits),
          city: data.city || "",
          state: data.state || ""
        };
        setCep(formatCep(next.zipCode));
        setHint(next.city ? `Entrega em ${next.city} - ${next.state}.` : "CEP encontrado.");
      }
      if (auth.isAuthenticated) {
        const saved = await auth.saveDeliveryCep(next.zipCode);
        next = { zipCode: saved.zipCode, city: saved.city, state: saved.state };
      } else {
        setNearPlace(next);
      }
      setDest(next);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="delivery-calc">
      <button
        type="button"
        className="delivery-toggle"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        <span>Calcular prazo de chegada</span>
        <span className={`delivery-caret ${open ? "open" : ""}`} aria-hidden="true">^</span>
      </button>
      {open && (
        <form className="delivery-panel" onSubmit={onCalculate}>
          <label htmlFor="prazo-cep">CEP
            <input
              id="prazo-cep"
              value={cep}
              onChange={(event) => {
                setCep(formatCep(event.target.value));
                setHint("");
                setError("");
              }}
              inputMode="numeric"
              autoComplete="postal-code"
              placeholder="14800-000"
              maxLength={9}
            />
          </label>
          {hint && <span className="field-hint">{hint}</span>}
          {hasDeliveryCep(auth.user) && !hint && (
            <span className="field-hint">Usando o CEP da sua conta.</span>
          )}
          {error && <p className="error" role="alert">{error}</p>}
          <button className="btn" type="submit" disabled={busy || cepDigits(cep).length !== 8}>
            {busy ? "Calculando..." : "Calcular"}
          </button>
          {eta && (
            <div className="delivery-eta">
              <strong>{eta.label}</strong>
              <span className="muted">{eta.shortLabel} · CEP {formatCep(dest.zipCode)}</span>
            </div>
          )}
        </form>
      )}
    </div>
  );
}
