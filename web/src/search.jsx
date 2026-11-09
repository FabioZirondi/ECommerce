import { useEffect, useId, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api, coverUrl, money } from "./api.js";
import { catalogHref } from "./catalog.js";
import { formatPlace, resolveNear, setNearPlace } from "./search.js";
import { useAuth } from "./auth.jsx";

export function SearchBox() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const [suggest, setSuggest] = useState(null);
  const boxRef = useRef(null);
  const listId = useId();
  const near = useMemo(() => resolveNear(auth.user), [auth.user]);

  useEffect(() => {
    setQuery(searchParams.get("q") ?? "");
  }, [searchParams]);

  useEffect(() => {
    const term = query.trim();
    if (term.length < 2) {
      setSuggest(null);
      setActive(-1);
      return undefined;
    }

    const controller = new AbortController();
    const timer = setTimeout(() => {
      const params = new URLSearchParams({ q: term });
      if (near?.city) params.set("nearCity", near.city);
      if (near?.state) params.set("nearState", near.state);
      api(`/api/products/suggest?${params}`, { signal: controller.signal, timeoutMs: 5000 })
        .then((data) => {
          setSuggest(data);
          setOpen(true);
          setActive(-1);
        })
        .catch((error) => {
          if (error?.name === "AbortError" || error?.name === "TimeoutError") return;
          setSuggest(null);
        });
    }, 180);

    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [query, near?.city, near?.state]);

  useEffect(() => {
    function onPointer(event) {
      if (!boxRef.current?.contains(event.target)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", onPointer);
    return () => document.removeEventListener("mousedown", onPointer);
  }, []);

  const options = useMemo(() => flattenSuggest(suggest), [suggest]);
  const showPanel = open && query.trim().length >= 2 && suggest;

  function goSearch(term = query) {
    const value = term.trim();
    setOpen(false);
    navigate(catalogHref(searchParams.get("c"), value));
  }

  function choose(option) {
    if (!option) return;
    setOpen(false);
    if (option.kind === "product" || option.kind === "nearby") {
      navigate(`/products/${option.id}`);
      return;
    }
    if (option.kind === "place") {
      setNearPlace({ city: option.city, state: option.state });
      setQuery(option.city);
      navigate(catalogHref(searchParams.get("c"), option.city));
      return;
    }
    if (option.kind === "category") {
      navigate(catalogHref(option.slug || option.id, query.trim()));
    }
  }

  function onSubmit(event) {
    event.preventDefault();
    if (showPanel && active >= 0 && options[active]) {
      choose(options[active]);
      return;
    }
    goSearch();
  }

  function onKeyDown(event) {
    if (!showPanel || options.length === 0) {
      if (event.key === "Escape") setOpen(false);
      return;
    }
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActive((index) => (index + 1) % options.length);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActive((index) => (index <= 0 ? options.length - 1 : index - 1));
    } else if (event.key === "Escape") {
      setOpen(false);
    }
  }

  return (
    <div className="search" ref={boxRef}>
      <form className="search-bar" onSubmit={onSubmit} autoComplete="off">
        <input
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
            setOpen(true);
          }}
          onFocus={() => query.trim().length >= 2 && suggest && setOpen(true)}
          onKeyDown={onKeyDown}
          placeholder="Buscar anúncios, cidade ou vendedor"
          aria-label="Buscar anúncios"
          aria-autocomplete="list"
          aria-expanded={Boolean(showPanel)}
          aria-controls={listId}
          aria-activedescendant={showPanel && active >= 0 ? `${listId}-${active}` : undefined}
          role="combobox"
        />
        <button type="submit">Buscar</button>
      </form>
      {showPanel && (
        <div className="search-suggest" id={listId} role="listbox" aria-label="Sugestões de busca">
          {options.length === 0 ? (
            <p className="search-suggest-empty">Nenhuma sugestão para “{query.trim()}”.</p>
          ) : (
            <>
              <SuggestSection title="Anúncios" items={suggest.products} kind="product" offset={offsetOf(options, "product")} active={active} setActive={setActive} listId={listId} onChoose={choose} />
              <SuggestSection title="Cidades próximas" items={suggest.places} kind="place" offset={offsetOf(options, "place")} active={active} setActive={setActive} listId={listId} onChoose={choose} />
              <SuggestSection
                title={near?.city ? `Perto de ${near.city}` : "Opções próximas"}
                items={suggest.nearby}
                kind="nearby"
                offset={offsetOf(options, "nearby")}
                active={active}
                setActive={setActive}
                listId={listId}
                onChoose={choose}
              />
              <SuggestSection title="Categorias" items={suggest.categories} kind="category" offset={offsetOf(options, "category")} active={active} setActive={setActive} listId={listId} onChoose={choose} />
            </>
          )}
        </div>
      )}
    </div>
  );
}

function flattenSuggest(suggest) {
  if (!suggest) return [];
  return [
    ...(suggest.products ?? []).map((item) => ({ kind: "product", ...item })),
    ...(suggest.places ?? []).map((item) => ({ kind: "place", ...item })),
    ...(suggest.nearby ?? []).map((item) => ({ kind: "nearby", id: item.id, ...item })),
    ...(suggest.categories ?? []).map((item) => ({ kind: "category", ...item }))
  ];
}

function offsetOf(options, kind) {
  return options.findIndex((option) => option.kind === kind);
}

function SuggestSection({ title, items, kind, offset, active, setActive, listId, onChoose }) {
  if (!items?.length || offset < 0) return null;
  return (
    <section className="search-suggest-group">
      <p className="search-suggest-label">{title}</p>
      <ul>
        {items.map((item, index) => {
          const optionIndex = offset + index;
          const selected = optionIndex === active;
          return (
            <li key={`${kind}-${item.id || item.city || item.slug}-${index}`}>
              <button
                type="button"
                id={`${listId}-${optionIndex}`}
                role="option"
                aria-selected={selected}
                className={`search-suggest-item ${selected ? "active" : ""}`}
                onMouseEnter={() => setActive(optionIndex)}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => onChoose({ kind: kind === "nearby" ? "product" : kind, ...item })}
              >
                <SuggestRow kind={kind} item={item} />
              </button>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function SuggestRow({ kind, item }) {
  if (kind === "place") {
    return (
      <>
        <span className="search-suggest-mark" aria-hidden="true">
          ⌖
        </span>
        <span className="search-suggest-copy">
          <strong>{item.label || item.city}</strong>
          <small>{item.count} anúncio(s) nesta cidade</small>
        </span>
      </>
    );
  }

  if (kind === "category") {
    return (
      <>
        <span className="search-suggest-mark" aria-hidden="true">
          ▦
        </span>
        <span className="search-suggest-copy">
          <strong>{item.name}</strong>
          <small>Categoria</small>
        </span>
      </>
    );
  }

  return (
    <>
      <img className="search-suggest-thumb" src={coverUrl(item)} alt="" />
      <span className="search-suggest-copy">
        <strong>{item.title}</strong>
        <small>
          {[item.hint, formatPlace(item.city, item.state), money(item.price)].filter(Boolean).join(" · ")}
        </small>
      </span>
    </>
  );
}
