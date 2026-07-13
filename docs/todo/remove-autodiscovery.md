# Sencilla — Restructuring & Hardening Working Doc

> **Status:** planning / backlog. Not yet started.
> **Scope of this doc:** (1) a code review + critique of `Sencilla.Core`, `Sencilla.Repository.EntityFramework`, and `Sencilla.Web`; (2) the plan to **remove auto-discovery** entirely; (3) the plan to **split repository contracts out of Core into `Sencilla.Repository`**; (4) performance / memory / API-design optimization notes.
> **How it was produced:** firsthand reads plus a 50-agent review where every *bug/thread-safety/critical* finding was adversarially verified against the source. **43 findings verified, 42 confirmed, 1 refuted.** Verification nuances (where the reproduced behavior differed from the first claim) are folded into each item below.
> **Reviewed at commit:** `48e7775`.

---

## Table of contents

- [Part A — Code review findings](#part-a--code-review-findings)
  - [A.1 Cross-cutting themes](#a1-cross-cutting-themes)
  - [A.2 Critical bugs (fix first)](#a2-critical-bugs-fix-first)
  - [A.3 Core — remaining confirmed bugs](#a3-core--remaining-confirmed-bugs)
  - [A.4 Repository.EntityFramework — remaining confirmed bugs](#a4-repositoryentityframework--remaining-confirmed-bugs)
  - [A.5 Web — remaining confirmed bugs](#a5-web--remaining-confirmed-bugs)
  - [A.6 Unverified findings (design / consistency, not adversarially checked)](#a6-unverified-findings-design--consistency-not-adversarially-checked)
- [Part B — Task 1: Remove auto-discovery](#part-b--task-1-remove-auto-discovery)
- [Part C — Task 2: Split Core → Sencilla.Repository](#part-c--task-2-split-core--sencillarepository)
- [Part D — Optimization: performance, memory, API design](#part-d--optimization-performance-memory-api-design)
- [Part E — Open decisions (the "grill list")](#part-e--open-decisions-the-grill-list)

---

# Part A — Code review findings

The **architecture is good**: capability-per-interface repositories, trait/marker entities, an event-driven constraint pipeline, thin controllers with centralized exception mapping. The problems are in execution details, and the dangerous ones fail **silently** (wrong behavior, no exception) — the worst failure mode for a framework because consumers can't see it.

## A.1 Cross-cutting themes

1. **Silent failure everywhere.** Missing command handler → no-op; missing event handler → nothing fires; missing repo → (mostly) 501 but sometimes NRE→500; derived-event publish → handler gets a blank event; missing filter/security handler → reads succeed *without* constraints. A framework should let the developer opt into strictness.
2. **Reflection on the hot path, uncached.** Every command/event dispatch does `MethodInfo.Invoke` + per-call string-join cache key + LINQ. Runs 1–2× per repository operation, i.e. many times per HTTP request.
3. **Registration built on fragile primitives.** `AppDomain.GetAssemblies()` load-timing, `StackFrame` caller detection, `Activator.CreateInstance`, unguarded `GetTypes()`, process-wide mutable statics. Task 1 (remove auto-discovery) is the opportunity to replace all of this.
4. **Docs contradict code.** `docs/core/{entities,repositories,filtering,dependency-injection}.md` describe APIs that don't compile against the current interfaces. Regenerate as part of any breaking change.
5. **Public-API spelling/naming freezes.** `Resolveable`, `Updator`/`Deletor`, `GetAvarage`, `IEntityDeleteable` in `IEntityDeletable.cs`, `buidler`. Rename **now**, during a breaking change — never cheaper.

---

## A.2 Critical bugs (fix first)

### C1 — `AddSencilla` only sees already-loaded assemblies · `libs/core/Bootstrap.cs:46`
`AppDomain.CurrentDomain.GetAssemblies()` returns only what the JIT has touched. .NET loads assemblies lazily, so a referenced `[assembly: AutoDiscovery]` library containing **only handlers/entities** (nothing statically referenced before `AddSencilla` runs) is silently absent — its `ITypeRegistrator`s never run, its services/handlers never register. No error; handlers just never fire, or resolution fails later with a confusing "no service registered." Whether it works depends on unrelated code layout (whether `Program.cs` happens to touch a type from that assembly first) → intermittent, hard to diagnose.
- **Verified:** confirmed; no compensating loader anywhere in `libs/`. Realistic victims are app-level plugin assemblies with no bootstrap call (framework packages usually load because the app calls their `AddSencillaXxx`).
- **Fix / note:** **Task 1 removes this whole mechanism** — the explicit-registration model is the fix. If any scanning survives, force-load the referenced closure (`DependencyContext` from `Microsoft.Extensions.DependencyModel`) or add an explicit `AddSencilla(params Assembly[])`.

### C2 — Singleton/scoped identity broken: one instance *per service type* · `libs/core/Injection/AutoDiscovery/AutoDiscoveryRegistrator.cs:47`
`RegisterType` calls `AddSingleton(i, type)` for each interface **and** `AddSingleton(type)` for the concrete type. MS.DI caches an instance **per `ServiceDescriptor`**, so a `[SingletonLifetime]` class implementing `IFoo`+`IBar` yields **three distinct "singletons."** Same for `[PerRequestLifetime]`/scoped: `ISystemVariable` and `SystemVariable` resolve to two different instances with separate dictionaries in one request — a `Set` via one is invisible via the other.
- **Verified:** confirmed. Currently **latent** in-repo (nothing resolves both a concrete type and its interface; no `[SingletonLifetime]` uses exist), but the registration enables it the moment app code resolves the same annotated type two ways.
- **Fix:** register concrete once, forward interfaces: `c.AddSingleton(type); foreach i: c.AddSingleton(i, sp => sp.GetRequiredService(type));` (same for scoped). Transient can keep the current form.

### C3 — Typed command dispatch silently returns `default` · `libs/core/Command/Contract/ICommandHandler.cs:30`
`ICommandHandler<in TCommand, TResponse>` inherits `ICommandHandlerBase<TCommand>` (the **one-arg** marker) instead of `ICommandHandlerBase<TCommand, TResponse>`. `CommandRegistrator` registers a handler only under the marker interfaces it finds, so `class H : ICommandHandler<MyCmd, MyResp>` is registered only as `ICommandHandlerBase<MyCmd>`. `CommandDispatcher.SendAsync<TResponse>` resolves `ICommandHandlerBase<,>`, gets null, and **returns `default(TResponse)` with no error.** Every response-returning command silently yields default unless the author *also* hand-implements the empty two-arg marker. No tests exercise this path.
- **Verified:** confirmed by code; no in-repo implementation or test guards it.
- **Fix:** `: ICommandHandlerBase<TCommand, TResponse>`. Add a round-trip `SendAsync<TResponse>` test.

### C4 — Infinite recursion in EF bootstrap → StackOverflow · `libs/repositories/EntityFramework/Bootstrap.cs:40`
The `IHostApplicationBuilder` overload body is `return builder.AddSencillaRepositoryForEF(configure);` on an `IHostApplicationBuilder` receiver. `IHostApplicationBuilder` isn't convertible to `IServiceCollection`, so overload resolution binds to **itself** → unbounded recursion, uncatchable `StackOverflowException`, process dies at startup for any caller of this public entry point.
- **Verified:** confirmed with a compiled repro. Only external consumers hit it (no in-repo caller uses this overload).
- **Fix:** `builder.Services.AddSencillaRepositoryForEF(configure); return builder;` + a test invoking the `IHostApplicationBuilder` overload.

### C5 — Dynamic-LINQ injection: raw user strings executed as query predicates · `libs/repositories/EntityFramework/Constraint/FilterConstraintHandler.cs:24`
`ToExpression` returns `prop.Query` verbatim when `prop.Type == null`, and `FilterTypeBinder.cs:68` (web) creates exactly such properties from **any unrecognized query-string key** (`filter?.AddProperty(propName, null)` — already flagged `// think how secure it is`). That string is executed via `System.Linq.Dynamic.Core` `query.Where(string)`. `GET /entities?Salary>100000||true` executes attacker-chosen predicates over any mapped property/navigation (boolean-oracle exfiltration, DoS via expensive expressions). The typed-value path (`vals.Append($"\"{v}\",")`, lines 90/101–104) also has naive quoting — a value containing `"` breaks out and injects expression text.
- **Verified:** confirmed end-to-end (binder → filter → handler → `Where(string)`); handler is registered for every `IBaseEntity` on read, so reachable.
- **Fix:** never pass user text to Dynamic LINQ. Whitelist `Query` against the EF model's real properties; pass values as substitution parameters (`Where("Prop == @0", value)`) instead of interpolating literals; drop or explicitly opt-in the raw-query passthrough. (See also Part D — moving to typed `Expression` composition kills this *and* the parse cost.)

### C6 — Delete events never published → security silently bypassed on delete · `libs/core/Entity/Deletable/EntityDeletingEvent.cs:7`
`EntityDeletingEvent<T>`/`EntityDeletedEvent<T>` are declared, `docs/core/entities.md` promises they fire, and `Security/Constraint/SecurityConstraint.cs` subscribes to `EntityDeletingEvent<T>` to authorize deletes — but **no repository ever publishes them** (`DeleteRepository.cs:42` has only a `// Add events` TODO; repo-wide grep finds zero publishers). Create/read/update are authorized; **delete is not**. Any caller reaching `IDeleteRepository.Delete` hard-deletes rows with no authorization event — unauthorized permanent data loss. (Both event files also carry copy-pasted "Fired when entity is updated" XML docs.)
- **Verified:** confirmed. `SecurityConstraint` is the sole delete guard and is gated on the never-fired event; `CrudApiController` exposes HTTP DELETE with no other guard.
- **Fix:** publish `EntityDeletingEvent` before and `EntityDeletedEvent` after deletion, on **both** the id-based `ExecuteDeleteAsync` path and the `RemoveRange` path (give handlers an `IQueryable` to narrow, like reads); or remove the event types + Security subscriptions until wired. Fix the XML docs.

---

## A.3 Core — remaining confirmed bugs

### Injection / bootstrap

- **Unguarded `GetTypes()` crashes bootstrap** · `Bootstrap.cs:50` (also `IServiceCollectionsExt.cs:16`). One unloadable type in any scanned assembly throws `ReflectionTypeLoadException` from framework startup with no hint which assembly. The repo *already knows* the fix — `BatchEntityRegistry.cs:34` wraps the same scan in `catch(ReflectionTypeLoadException)` and recovers via `ex.Types`. **Fix:** helper that catches and returns `ex.Types.Where(t => t != null)`, logging the assembly. *(Moot if Task 1 removes scanning.)*
- **`[Implement]` + `AutoDiscoveryRegistrator` double-register the same mapping** · `ImplementAttributeRegistrator.cs:16`. `GetServices<IFoo>()` returns the impl twice → an event handler resolved via `GetServices` **executes twice per event** (duplicate emails/writes); `[Implement(PerRequest=true)]` produces one Scoped + one Transient descriptor whose winner depends on unspecified `GetTypes()` order. **Note:** the reviewer found this is broader than `[Implement]` — `EventRegistrator` + `AutoDiscoveryRegistrator` already double-register `IEventHandlerBase<T>` for *every* handler, so **all in-memory event handlers currently run twice per event.** **Fix:** make the registrars mutually exclusive (`AutoDiscoveryRegistrator` skips `[Implement]` types like it skips `[DisableInjection]`), or use `TryAddEnumerable` with defined lifetime precedence. *(Task 1 should resolve the whole overlap.)*
- **`StackFrame` calling-assembly detection breaks under inlining/trim/AOT, then NREs as a dict key** · `IServiceCollectionsExt.cs:10`. `new StackFrame(1).GetMethod()` can belong to a different assembly (inlining) or return null (AOT); `CallingAssembly = assembly!` hides the null, which later throws `ArgumentNullException` as a `Dictionary<Assembly,…>` key inside the `configure` callback, far from the cause. No `[MethodImpl(NoInlining)]` anywhere. **Fix:** take `Assembly` explicitly, or `Assembly.GetCallingAssembly()` + `[MethodImpl(NoInlining)]` + non-null validation.
- **`InvokeMethod<T>` casts `Task.CompletedTask` to `Task<T>` → `InvalidCastException`** · `IServiceProviderExt.cs:24`. When the method isn't found (a supported, tested case for the non-generic overload) the "graceful null" path throws instead of returning default; also throws when a located handler returns non-generic `Task` but the caller used `InvokeMethod<TResponse>`. **Fix:** null-check `methodInfo` and return `default`; validate `ReturnType` is `Task<T>` or throw a descriptive error.
- **`SystemVariable.Get<T>` NREs for value-type `T` when unset** · `SystemVariables/Impl/SystemVariable.cs:43`. `(T?)obj` compiles to an unbox for unconstrained `T`; on a `TryGetValue` miss `obj` is null → unboxing null to `int`/`bool` throws NRE (while the whitespace-name branch correctly returns default). Latent in-repo (callers use reference types only). **Fix:** `return obj is T typed ? typed : default;`.
- **`Activator.CreateInstance` on a registrator with ctor params throws `MissingMethodException`** · `Bootstrap.cs:60`. Registrators are instantiated before the container exists; a DI-dependent `ITypeRegistrator` (the docs even show one decorated with `[Implement]`) crashes `AddSencilla`. (Correction: the exception *does* name the type on modern .NET — the defect is the unguarded startup crash, not a context-free message.) **Fix:** try/catch → `InvalidOperationException` naming the type + the parameterless-ctor requirement; document it.
- **Constructor validation inspects only `GetConstructors().FirstOrDefault()`** · `AutoDiscoveryRegistrator.cs:20`. A class with two public ctors (one `(string)` for tests, one DI-resolvable) is silently skipped when the string one enumerates first; any struct param (`Guid`, `DateTime`, enum) also fails the `IsClass||IsInterface` test. Failure surfaces as a runtime "Unable to resolve service." **Fix:** register if **any** ctor passes; log skips at debug.
- **Parameter matching uses exact runtime-type equality; NREs on null args** · `IServiceProviderExt.cs:92`. `p.GetType() == param.ParameterType` (and the FullName cache key at line 36) means (1) any null element in `@params` NREs, and (2) a derived instance where the method takes the base type fails to match → tries DI → "Cannot resolve parameter." **Fix:** `param.ParameterType.IsInstanceOfType(p)` + null guards. (Command path is safe — MS.DI matches closed generics exactly; the reachable cases are null args and the **event** path below.)

### Event / command / serialization / extensions

- **Derived event through base-typed `PublishAsync<T>` mis-delivers** · `Event/Impl/InMemoryMiddleware.cs:18`. Same exact-type matching: publishing `new OrderCreated()` as `PublishAsync<OrderEvent>(...)` doesn't bind the arg. **Verified nuance:** under standard bootstrap it does **not** throw — `AutoDiscoveryRegistrator` registers concrete event classes as transient, so DI hands the handler a **fresh blank `OrderEvent`** (silent data loss). It throws "Cannot resolve parameter" only when the event type isn't in DI. **Fix:** `IsInstanceOfType` matching.
- **One throwing handler aborts the whole fan-out** · `InMemoryMiddleware.cs:17` (and `EventDispatcher.cs:8`). Sequential `foreach` with no try/catch: the first thrower skips remaining handlers *and* all later middlewares, and the exception propagates out of the repo call **after** the entity was saved. **Verified nuance:** for pre-op `-ing` events this abort-on-throw is the *intended* veto (SecurityConstraint) — keep it there; the problem is `-ed` notification events. **Fix:** for `-ed` events, catch per-handler, continue, aggregate into `AggregateException` (or log-and-continue). Document the fan-out contract.
- **JSON string converters emit no value for empty strings → malformed JSON** · `JsonObjectStringConverter.cs:21` and `JsonArrayStringConverter.cs:21`. On `""`/whitespace, `Write` returns without writing, leaving a dangling property name. **Verified nuance:** `JsonSerializer` uses `SkipValidation=true` in release, so no `InvalidOperationException` — it **silently emits unparseable JSON** (e.g. `{"Attrs":"After":42}`), so an API returns 200 with a body the client can't parse. `HandleNull` is false, so null is unaffected — only empty/whitespace. `JsonArrayStringConverter.Read` *also* silently drops data for a string token like `"[1,2]"`. Live on `User.Attrs` (a `[CrudApi]` entity). **Fix:** `WriteNullValue()` (or `{}`/`[]`) in every path; in `Read` throw `JsonException` or accept a JSON string token.
- **Enum-array converter breaks `[Flags]` and non-int enums** · `JsonArrayConverter.cs:14`. `Read` rejects any value failing `Enum.IsDefined` → a `[Flags]` combo (`Read|Write=3`) this converter itself wrote won't round-trip; `Write` uses `Convert.ToInt32` → `OverflowException` for `long`/`ulong` enums; `Enum.IsDefined(int)` even throws for non-int underlying types. Latent (no `[JsonArray]` in repo). **Fix:** use the underlying type / `long`, skip `IsDefined` for `[Flags]`, write with `Convert.ToInt64`.
- **`JsonArrayAttribute` IsEnum guard is dead code** · `JsonArrayAttribute.cs:6`. `base(MakeGenericType(enumType))` runs before the ctor body and `MakeGenericType` itself throws for non-enums (the `struct, Enum` constraint), so the intended validation never runs; also `new ArgumentException(nameof(enumType))` passes the param name as the *message*. **Fix:** validate in a static helper called inside `base(...)`; use the two-arg `ArgumentException(message, paramName)`.
- **`IEnumerableEx.StartWith` never disposes its enumerators** · `Extensions/IEnumerableEx.cs:34`. All three overloads `GetEnumerator()` on both sequences and return from multiple paths with no `using`; resource-owning enumerators (EF readers, `File.ReadLines`) leak. Latent (only in-repo caller passes arrays). **Fix:** `using var` on both.
- **`StartWith` keySelector overload compares element nullness, not key nullness** · `IEnumerableEx.cs:72`. Tests `fEnum.Current == null` instead of `keySelector(fEnum.Current) == null` → a non-null element with a null key wrongly returns false; a null element is treated as a key match without calling the selector. **Fix:** compute the key once per iteration and compare that.

---

## A.4 Repository.EntityFramework — remaining confirmed bugs

- **Filter values formatted culture-sensitively / unquoted → invalid or wrong queries** · `FilterConstraintHandler.cs:104`. Non-string/Guid values appended as `$"{v},"`. `DateTime` → `Prop in (5/22/2026 12:00:00 AM)` → `ParseException`. `decimal`/`double` use thread culture — on a comma-decimal server `1.5` renders `1,5` and is **silently parsed as two values 1 and 5** (wrong results, no error). **Verified nuance:** the `bool` sub-claim is **wrong** — Dynamic LINQ treats `true`/`false` case-insensitively, so booleans work. **Fix:** `CultureInfo.InvariantCulture` + per-type quoting, or Dynamic LINQ `@0` placeholders (also fixes C5).
- **Create/Upsert/Merge (and `UpdateRepository.Update`) enumerate the caller's `IEnumerable` 5× ** · `CreateRepository.cs:42`. `entities.AsQueryable()` (deferred) + the `CreatedDate` foreach + `AddRange` + created-event + return each re-enumerate. A lazy `Select` source builds **fresh instances per pass**: `CreatedDate` is stamped on discarded objects, inserted rows have no timestamp, returned entities lack DB-generated Ids. `GetOrCreateAsync` already does `.ToList()` — the others don't. **Fix:** materialize once at the top (`var list = entities as IReadOnlyList<T> ?? entities.ToList();`).
- **Append-only int-keyed entities never get the shorthand repo registrations** · `Extension/IServiceCollectionEx.cs:128`. `RegisterCreateRepo`/`RegisterUpdateRepo` return early for `IEntityAppendOnlyTrack` before the `key==int` shorthand registration, and `AppendOnlyTrackRepository<T,TContext,TKey>` implements no shorthand interface. Injecting `IUpdateRepository<MyRate>` (or `SencillaAppExt.Creator<T>()/Updator<T>()`) throws `InvalidOperationException`. **Verified nuance:** the built-in CRUD endpoints (`CrudApiController`, `BatchEntityInvoker`) use the **two-generic** forms and are unaffected — impact is consumer injection + `SencillaAppExt`. **Fix:** add an int-key shorthand `AppendOnlyTrackRepository<TEntity,TContext>` implementing the one-generic interfaces and register when `key == typeof(int)`.
- **Supersede with two same-business-key entities in one batch inserts two open versions** · `AppendOnlyTrackRepository.cs:72`. Phase 1 closes open rows per-entity but `Save()` only after the loop; the second iteration's tracking query (no `AsNoTracking`) matches the still-NULL DB row and identity-resolves to the already-tracked prior, overwriting its `ActiveTo`. Phase 2 inserts **both** new versions with `ActiveTo==null`. Without the (only "recommended") unique index → two open rows, permanent valid-time corruption; with it → deterministic `DbUpdateException` on all 3 retries. Reachable from `CrudApiController.CreateMany/UpdateMany` with no dedup. **Fix:** detect duplicate business keys up front (reject with `BadRequestException` or process sequentially / close prior in-memory).
- **`Create(entity, token)` / `Update(entity, token)` silently drop the token** · `CreateRepository.cs:31`, `UpdateRepository.cs:24`. They call `Create(new[]{entity})` / `Update([entity])`, which bind to the `params` overload hardcoding `CancellationToken.None`. Most-used single-entity path; aborted HTTP requests aren't honored. **Fix:** pass the token (`RemoveRepository` already does).
- **`StackFrame(1)` caller detection unreliable in Release** · `Bootstrap.cs:58` (+ `AddSencillaRepositories` line 45). No `[MethodImpl(NoInlining)]`; discovery silently registers nothing from the intended assembly. The defensive `GetCallingAssembly()` frame-walker (line 92) is **dead code**. **Verified nuance:** with tiered compilation, startup code is Tier-0 (minimal inlining), so this mainly bites under disabled tiering / R2R / AOT / an inlined user wrapper. **Fix:** explicit `Assembly` arg (or generic anchor `AddSencillaRepositories<TMarker>()`); wire up or delete the dead walker. *(Task 1 territory.)*
- **Process-wide mutable statics leak across hosts** · `Bootstrap.cs:35` (`Entities`/`Assemblies`) + `DynamicDbContext.cs:6` (`_compiledModel`). Static `List<T>` mutated at registration + enumerated in `OnModelCreating`; a second host in the same process gets the **union** of entities or the **first** host's pinned model. `OnModelCreating`'s plain `foreach` can throw "collection was modified" under parallel test registration (already happens in `MergeQueryBuilderTests`). **Fix:** move the registry into an options object on the specific `IServiceCollection`; key the compiled-model cache per provider. *(This is also Task-1 / Task-2 cleanup — the shared entity registry.)*
- **`RegisterEFContexts` picks the `AddDbContext` overload by `First()` without arity check** · `IServiceCollectionEx.cs:39`. The predicate matches both the 1-generic and 2-generic overloads (identical param lists); if the 2-generic one enumerates first, `MakeGenericMethod(one-arg)` throws. **Verified nuance:** latent today (CoreCLR returns declaration order, 1-generic is declared first). **Fix:** `&& m.GetGenericArguments().Length == 1`; cache the `MethodInfo`.
- **`RegisterEFRepositoriesForType` crashes discovery for any `IBaseEntity` lacking `IEntity<TKey>`** · `IServiceCollectionEx.cs:60`. `.First(i => … == typeof(IEntity<>))` throws a type-nameless `InvalidOperationException` for a class implementing a marker (`IEntitySnapshot`, `IEntityHideable`) without `IEntity<TKey>`; one such class anywhere aborts startup. Latent (no such class in-repo). **Fix:** `FirstOrDefault` + skip, or throw naming the offending type.
- **`IsOnlyActiveToClose` permits backdating `ActiveTo` into the past** · `Interceptor/AppendOnlyInterceptor.cs:97`. Checks only `OriginalValue==null`, never `CurrentValue`, unlike sibling `IsOnlyActiveToFutureReopen` (`newTo > now`). A direct `DbContext` write can set an open version's `ActiveTo` years in the past — the exact history-rewrite the interceptor exists to prevent; the reading handler then hides it from as-of reads. Repo path is safe (Supersede snaps to now). **Fix:** require `CurrentValue is DateTime newTo && newTo >= now` (pass `now` in) — with a small tolerance, since Supersede's `now` is captured microseconds earlier.
- **`ToIncludePath` silently drops unresolved segments + per-request reflection** · `FilterConstraintHandler.cs:48`. `with="Author.Publisher"` with a typo (or a collection segment like `List<Book>` whose element type is never unwrapped) silently includes only `Author`. Collection navigations are effectively unsupported. `with` is user-controlled via the web binder, so any navigation can be eager-loaded by clients. **Fix:** unwrap `IEnumerable<T>` element types, fail/log on unresolved segment, cache paths per `(entityType, with)`, whitelist exposable navigations.

---

## A.5 Web — remaining confirmed bugs

- **`[UseCaching]` cache key is identity-hashed → 0% hit rate + unbounded growth** · `Api/CrudApiController.cs:28`. Key uses `filter.GetHashCode()`; `Filter` overrides no `GetHashCode`/`Equals`, and the binder makes a fresh `Filter` per request → every key unique. The repo query runs every time anyway, and each request adds a `MemoryCache` entry living for the expiration (60 min for the 9 entities using `[UseCaching(60)]`) — pure memory bloat, feature never works. A rare hash collision could even serve a wrong entity's cached result. **Fix:** key on the filter's semantic content (canonical serialization) or implement `Equals`/`GetHashCode` on `Filter`.
- **Body filter binder casts to Newtonsoft `JObject` but the serializer is System.Text.Json** · `Binder/FilterTypeBodyBinder.cs:82`. `(JObject)result.Model` — STJ deserializes an `object` target to a boxed `JsonElement`, never a `JObject`. `CheckType` also compares JTokenType names (`"Integer"`) to CLR names (`"Int32"`), dropping all numeric values even on the Newtonsoft path. **Verified nuance:** with the default non-buffered body the earlier re-read already fails (drained stream), so it usually **silently binds nothing** rather than throwing; the `InvalidCastException` needs `Request.EnableBuffering`. Either way the class is broken/dead for the default serializer, and it violates the repo's no-Newtonsoft standard. **Fix:** rewrite against `JsonElement`/`JsonDocument`; map `JsonValueKind` to CLR types; drop Newtonsoft.
- **Query filter binder throws `KeyNotFoundException` for array-typed entity properties** · `Binder/FilterTypeBinder.cs:49`. Provider populates `arrayEntityProperties` only for **non-array** properties, but `EntityProperties` contains all; a query param matching an array-typed property hits an unguarded `EntityArrayProperties[fullName]` → `KeyNotFoundException` → 500. **Verified nuance:** the companion NRE claim for `?price=abc` is **wrong** — ASP.NET's `ArrayModelBinder` returns Success with a non-null `default(T)` array (the real defect there is a bogus default silently added to the filter). Latent (no `[CrudApi]` entity currently has an array property). **Fix:** guard/skip array-typed properties consistently in provider + binder; null-check `result.Model`.
- **`CrudApiControllerFeatureProvider` scans every loaded assembly's `ExportedTypes` → can abort MVC startup** · `Provider/CrudApiControllerFeatureProvider.cs:7`. `AppDomain…SelectMany(a => a.ExportedTypes)` with no guard; `ExportedTypes` throws `FileNotFoundException`/`TypeLoadException`/`NotSupportedException` for an assembly with an unresolvable dependency or a dynamic assembly — inside the `ApplicationPartManager` feature provider, so app init fails. Ignores the `parts` parameter and rescans everything each call. **Fix:** iterate the supplied `parts`, or wrap per-assembly enumeration in try/catch. *(Task 1: replace with the entity registry or `AddCrudApiFor<T>()`.)*
- **`Content-Disposition` filename unencoded/unquoted** · `Results/FileCallbackResult.cs:19`. `filename={FileDownloadName}` raw — spaces/`;`/`,` truncate the header, non-ASCII isn't RFC-6266 encoded, CR/LF is a header-injection vector (Kestrel throws → 500). `ContentType.ToString()` is redundant. **Fix:** build with `ContentDispositionHeaderValue` + `FileNameStar`.
- **Cached `GetAll` path bypasses the null-repo guard → NRE/500** · `Api/CrudApiController.cs:33`. Non-cached path returns 501 via `FromService`; the cached branch does `repo!.GetAll(...)` — a `[UseCaching]` entity with `IMemoryCache` registered but no read repo (e.g. `UserType`, `Country`) throws NRE → 500 instead of 501. **Fix:** resolve + null-check before `GetOrCreateAsync`, return `NotImplemented()`.
- **`SuppressDiagnosticsCallback` silences logging for ALL `SencillaException`s, including 500s** · `Bootstrap.cs:69`. Suppresses on `c.Exception is SencillaException`, but `SencillaExceptionHandler` maps unrecognized subtypes (`InternalServerErrorException`, bare `SencillaException`) to 500 and writes `exception.Message` into ProblemDetails — so genuine 500s go to the client with the message **and** produce no server-side log. **Fix:** suppress only non-5xx-mapped exceptions; omit `Message` from 500 ProblemDetails.

---

## A.6 Unverified findings (design / consistency, not adversarially checked)

These are lower-severity or design-level and were **not** put through the refute pass. Treat as leads, verify before acting.

**Core — injection**
- `AddSencillaAutoDiscovery` uses `TryAddTransient` → only the **first** implementation of each interface is registered, rest silently dropped; `t.IsGenericType` filter excludes open generics entirely (`IServiceCollectionsExt.cs:34`). Use `TryAddEnumerable`. **Directly relevant to Task 1's `RegisterFor<T>` design.**
- Opt-out (`[DisableInjection]`) rather than opt-in registration bloats the container with every POCO/DTO/entity; `Add` (not `TryAdd`) means a double `AddSencilla` doubles everything (`AutoDiscoveryRegistrator.cs:8`).
- Per-dispatch cache-key string allocation on the hot path (`IServiceProviderExt.cs:36`); deferred `assemblies`/`types` enumerated twice at startup (`Bootstrap.cs:71`). See Part D.
- Doc/code lifetime contradictions; `[Implement]` examples don't compile (`PerRequestLifetimeAttribute.cs:8`).
- `AutoDiscoveryOptions` in the **global namespace** (`Injection/Entity/AutoDiscoveryOptions.cs:2`).
- `StarterKit.Run` param typo `buidler`; args-less overload discards command-line config (`StarterKit.cs:6`). `AddSencilla(IServiceCollection, IConfiguration)` never uses the config (`Bootstrap.cs:42`). `Resolveable` misspelled + opaque `R<T>()`/`All<T>()` (`Resolveable.cs:8`).

**Core — entity / repository contracts**
- `IHideRepository.Hide/Show(IEnumerable)` return `Task<TEntity>` (should be a collection); **no live implementation** (EF has none, SqlMapper commented out) yet `BatchEntityInvoker` resolves it (`IHideRepository.cs:28`).
- `IEntityParentable<TKey>` ties `ParentId` to the non-nullable `TKey` → tree roots can't have null `ParentId`; docs show `int? ParentId` which doesn't compile (`Parentable/IEntityParentable.cs:8`).
- `docs/core/{filtering,entities,repositories}.md` describe non-existent APIs throughout — following any example fails to compile (`filtering.md:38`).
- `EntityBaseEvent<T>.Entities` is a nullable settable `IQueryable` with **two incompatible meanings** (query-rewrite extension point for reads vs read-only payload for writes) and no null guard in the read pipeline (`EntityBaseEvent.cs:14`). Split read vs write event contracts.
- Optional `CancellationToken` **before** `params with` makes include overloads unusable without an explicit token; params annotated `[]?` needlessly; XML `includes` param name is stale (`IReadRepository.cs:29`).
- `GetAvarage` misspelling in the public contract; aggregates return untyped `Task<object>` (boxing + casts); `GetAvarage` hardcodes `double` (precision loss for money) (`IReadRepository.cs:67`).
- `Filter`/`FilterProperty` fully mutable and shared by reference through the event pipeline → handlers can mutate the caller's filter; reuse accumulates values (`FilterProperty.cs:22`). `IFilter`/`Filter` disagree on `AddProperty` nullability; `AddQuery`/`GetProperty` only on the concrete class; repeated `AddProperty` silently drops a conflicting type (`Filter.cs:33`).
- `params` write overloads drop `CancellationToken` and shuffle arg order vs siblings (`ICreateRepository.cs:33`).
- `Detach`/`ClearChangeTracker`/`JsonMergeAsync` leak EF specifics into the storage-agnostic contract — **the main obstacle to shipping `Sencilla.Repository` as provider-neutral** (`IUpdateRepository.cs:39`). Move to an EF-only capability interface.
- Misspellings/mismatches: `SencillaAppRepositoryExt` in `SencillaAppExt.cs`, `Updator`/`Deletor`, `IEntityDeleteable` in `IEntityDeletable.cs`, `IEntityRemoveable`, `Delete(IEnumerable<TEntity> ids)` param named `ids` (`SencillaAppExt.cs:6`).
- No Sencilla exception accepts an inner exception → wrapping destroys the cause (`SencillaException.cs:10`).
- `IEntityPublishable` exposes a magic `byte PublishStatus` and forces `byte[] RowVersion` (EF concurrency detail) onto a domain trait (`Publishable/IEntityPublishable.cs:8`).

**Core — event / command / serialization**
- Missing command handler is a silent no-op in both `SendAsync` overloads (`CommandDispatcher.cs:19`). Consider throwing (MediatR-style) or a `TrySendAsync`.
- Event resolution keyed on **static** `T`, not runtime type — publishing as `IEvent` resolves zero handlers; inconsistent with `CommandDispatcher` which uses `GetType()` (`EventDispatcher.cs:6`).
- Per-dispatch reflection `Invoke` + string/LINQ allocation on the framework's hottest path (`CommandDispatcher.cs:20`). Compile a cached invoker delegate. See Part D.
- Malformed Guid → `FormatException` not `JsonException` → 500 instead of 400 (`EmptyOrNullGuidConverter.cs:30`, `NullableEmptyGuidConverter.cs:27`). Use `TryParse`.
- `ExpressionEx.OrElse/AndAlso` on a null receiver return null → **silently discards all predicates** (data exposure in the exact security-filter composition it exists for; `SecurityConstraint` already hand-guards around it) (`ExpressionEx.cs:9`).
- `ICommandMiddleware` is a dead abstraction — commands bypass middleware (`ICommandMiddleware.cs:13`). `CommandRegistrator` namespace `Sencilla.Core.Impl` vs the rest `Sencilla.Core` (`CommandRegistrator.cs:1`); same for `JsonArrayAttribute`/`JsonArrayConverter` (`JsonArrayAttribute.cs:1`). `ContainsAll` (a string helper) misplaced in `IEnumerableEx` (`IEnumerableEx.cs:127`).

**Repository.EntityFramework**
- `GetCount` + aggregates apply the filter's `Skip`/`Take` → counting a paged filter returns ≤ page size, not the total (`ReadRepository.cs:75`). Build the count/aggregate query without paging.
- `GetSum/Max/Min/Avarage` run **synchronously** inside async methods (blocking I/O), ignore the token, and NRE when `filter`/`Aggregate` is null (`ReadRepository.cs:82`).
- **No read path filters soft-removed (`DeletedDate`) or hidden (`IEntityHideable`) rows** — every query returns them; consumers must filter manually everywhere (`ReadRepository.cs:115`). Add a reading-event handler or EF global query filter.
- `Delete(ids)` hard-deletes with **no constraint check and no events** → row-level security on reads doesn't protect deletes; the `Save` after `ExecuteDeleteAsync` is dead work (`DeleteRepository.cs:31`). (Pairs with C6.)
- `Current()`/`AsOf()` helpers build on `repo.Query` (raw DbSet) → **bypass the reading-event pipeline** (security + filter constraints); same for `ReadRepository.Where` (`AppendOnlyTrackRepositoryEx.cs:23`). Route through the event-publishing path or clearly mark as unconstrained.
- `ExecuteUpdateAsync` extension overloads accept a token but never forward it; `UpsertBulkAsync`/`MergeBulkAsync` have no token at all (`UpdateRepoEx.cs:60`).
- `IFilter.Descending` silently ignored (sorting only via the undocumented `Name|desc` pipe micro-syntax); unknown `OrderBy` names → `ParseException`/500 (`ReadRepository.cs:128`).
- `BusinessKeys.Match` embeds values as `Expression.Constant` → EF inlines them, defeating SQL parameterization → plan-cache pollution on the hot supersede path (`BusinessKeys.cs:34`).
- `DynamicDbContext` hardcodes SQL Server `dbo` schema despite provider-agnostic intent; `JsonMergeAsync` is SQL-Server-only but exposed as a generic extension (`DynamicDbContext.cs:62`).

**Web**
- `CrudApiAttribute.Cache` is dead — caching is driven by the separate `[UseCaching]` (`CrudApiAttribute.cs:21`).
- `GetById` ignores its `filter` and has no token; `UpdateOne/Many` drop the request token; `CreateOne/UpsertOne/MergeOne/GetOrCreateOne` never apply the route `{id}` to the body (`CrudApiController.cs:43`).
- Per-request O(params × properties) linear scans over metadata dictionaries during model binding (`FilterTypeBinder.cs:33`). Precompute an `OrdinalIgnoreCase` name→binder map.

---

# Part B — Task 1: Remove auto-discovery

**Goal:** no implicit assembly scanning. Users register explicitly, or use an interface-scoped sweep `services.RegisterFor<ISomeInterface>()` (generalizing the already-present but zero-caller `AddSencillaAutoDiscovery` / `AutoDiscoveryOptions.For<T>`).

## B.1 `[assembly: AutoDiscovery]` — 10 framework assemblies carry it

| # | File | Assembly |
|---|------|----------|
| 1 | `libs/core/Bootstrap.cs:21` | Sencilla.Core |
| 2 | `libs/web/Bootstrap.cs:29` | Sencilla.Web |
| 3 | `libs/extensions/EntityFrameworkCore/Bootstrap.cs:12` | Sencilla.Repository.EntityFramework.Extension |
| 4 | `libs/repositories/EntityFramework/Bootstrap.cs:29` | Sencilla.Repository.EntityFramework |
| 5 | `libs/files/Core/Bootstrap.cs:20` | Sencilla.Component.Files |
| 6 | `libs/components/Config/Bootstrap.cs:4` | Sencilla.Component.Config |
| 7 | `libs/components/Security/Bootstrap.cs:15` | Sencilla.Component.Security |
| 8 | `libs/components/Users/Bootstrap.cs:18` | Sencilla.Component.Users |
| 9 | `libs/components/I18n/Bootstrap.cs:19` | Sencilla.Component.I18n |
| 10 | `libs/components/Geography/Bootstrap.cs:6` | Sencilla.Component.Geography |

No apps or tests carry it. Not marked (already scan-free at assembly level): authentication, messaging, scheduler, mappers, mobile, webapi, files storage providers.

## B.2 The 8 `ITypeRegistrator` implementations

| Registrator | Package / file | Scans for | Registers | Explicit replacement |
|---|---|---|---|---|
| **AutoDiscoveryRegistrator** | core, `Injection/AutoDiscovery/…` | every concrete non-generic class w/o `[DisableInjection]`, ctor with only class/interface params | type + direct interfaces; lifetime from attrs (`PerRequest`→Scoped), default Transient, `Add*` | Per-package `AddSencillaXxx()` + `RegisterFor<TInterface>(asm)`. Widest silent fallout on removal. |
| **ImplementAttributeRegistrator** | core, `Injection/Impl/…` | `[Implement(typeof(I))]` classes | `AddScoped/Transient(attr.Interface, type)` | **Zero call sites in repo** — delete outright. |
| **CommandRegistrator** | core, `Command/Impl/…` | `ICommandHandlerBase<>` / `<,>` implementors | `AddTransient(closedHandlerInterface, type)` | `RegisterFor<ICommandHandlerBase<…>>(asm)` sweep. |
| **EventRegistrator** | core, `Event/Impl/…` | `IEventHandlerBase<>` implementors | `AddTransient(closedHandlerInterface, type)` | Same. |
| **SecurityConstraintRegistrator** | components/Security | every `IBaseEntity` class | reading/creating event handlers per entity | `AddSencillaSecurityForType(assembly)` exists — call per entity assembly. |
| **SecurityAttributeDiscoverer** | components/Security | `[AllowAccess]` classes | collects a `Matrix` permission list into its **own instance**, later injected as a singleton | **Needs redesign** — build the list and register it as a normal singleton. |
| **FilterConstraintRegistrator** | repositories/EF | every `IBaseEntity` class | `FilterConstraintHandler<T>` (+ append-only reading handler) per entity | `RegisterEFFilters(Type)` exists but is **commented out** of the explicit path (`Bootstrap.cs:83`). |
| **RepositoryRegistrator** | repositories/EF | every `IBaseEntity` class | 5 CRUD repos + **appends to static `RepositoryEntityFrameworkBootstrap.Entities`** (sole input to `DynamicDbContext.OnModelCreating`) | `AddSencillaEFRepositoryForAssembly(asm, cfg)` exists. Static side effect is the critical hidden coupling. |

> **Hidden coupling:** `AddSencilla` registers each registrator instance as a DI singleton, and `SecurityAttributeDeclaration` resolves `SecurityAttributeDiscoverer` from the container. Removing the scan breaks that ctor chain even if everything else is registered.

## B.3 Attribute usage (all become dead after removal)

| Attribute | Uses | Where |
|---|---|---|
| `[Implement]` | **0** | defined + registrator + documented, never used |
| `[SingletonLifetime]` | **0** | none |
| `[PerRequestLifetime]` | **2** | core `SystemVariable`; EF `RepositoryDependency` (also explicitly `TryAddScoped`, so only the core one is load-bearing) |
| `[DisableInjection]` | **27** | files/Core 18; web 3; EF 3; core 2; Users 1 — all pure opt-outs, deletable after removal |

Doc drift to fix: `docs/core/dependency-injection.md` inverts the lifetime table and shows non-compiling class-level `[AutoDiscovery]`.

## B.4 Existing explicit-registration surface (what's already there)

- **Core:** `AddSencilla(IHostApplicationBuilder)`, `AddSencilla(IServiceCollection, IConfiguration)` *(the scanner)*, `AddSencillaAutoDiscovery(IServiceCollection, Action<AutoDiscoveryOptions>)` — `AutoDiscoveryOptions.For<T>()/For(Type)/For(Assembly, Type)`, **zero callers, this is the `RegisterFor<T>` prototype**.
- **EF:** `AddSencillaRepositoryForEF` (both overloads — **the IHostApplicationBuilder one is the recursion bug C4**), `AddSencillaRepositories` (StackFrame), `AddSencillaEFRepositoryForAssemblies/ForAssembly`, `RegisterEFFilters(Type)`, `RegisterEFContexts(Type, cfg)`, `RegisterEFRepositoriesForType(Type, out bool)`, `WarmUpEFModel(IHost)`.
- **Web/WebApi:** `AddSencillaWeb(IMvcBuilder)`, `AddSencillaBatch(...)`, `AddSencillaEndpoints(Assembly)` + `MapSencillaEndpoints(...)` *(MinimalApi — already the desired explicit interface-scoped sweep)*.
- **Components:** `AddSencillaSecurity` (no-op), `AddSencillaSecurityForType(Assembly?)`, `AddSencillaSecurityFromAttributes(Type, List<Matrix>)`, `AddSencillaSecurityFromDatabase(Type)`, `AddSencillaUsers` (no-op), `AddI18n(...)`, `AddGeography` (no-op).
- **Files:** already fully explicit (`AddSencillaFiles` + per-provider `UseLocalDrive/UseAzureStorage/…`).
- **Scheduler / Messaging / Authentication:** explicit config-object patterns. **`AddSencillaAuthentication` is the model to converge on** — no scanning, registers its own handlers by hand.
- **Other implicit scans NOT gated by `[AutoDiscovery]`** (decide if in scope): `CrudApiControllerFeatureProvider` (all assemblies, `[CrudApi]`), `BatchEntityRegistry` (`[SencillaEntity]`), scheduler AppDomain fallback, and the `StackFrame` caller detection sites.

## B.5 Blast radius

- **apps/** = only `Directory.Build.props`. **No sample apps. Zero migration.**
- **tests/** (19 projects) — **none use the global scan**; they already use explicit paths (`RegisterEFRepositoriesForType`, `AddSencillaMessaging(assemblies)`, manual `ServiceCollection`s). ~zero test work.
- **docs/** — 7+ files teach the scan; all need rewriting.
- **Real blast radius = external consumers** (e.g. the Photoboost app) following the documented `AddSencilla()` + `[assembly: AutoDiscovery]` pattern.
- **`libs/autodiscovery/`** — completely **empty dead directory**. Delete, or make it the home of the new `Sencilla.Discovery` helper (`RegisterFor<T>`).

## B.6 Minimal replacement surface

```csharp
// interface-scoped, assembly-explicit sweep; supports open generic interface defs
IServiceCollection RegisterFor<TInterface>(this IServiceCollection s, Assembly assembly, ServiceLifetime lifetime = Transient);
IServiceCollection RegisterFor(this IServiceCollection s, Type interfaceType, Assembly assembly, ServiceLifetime lifetime = Transient);
```

Plus **one entity-registry primitive** to replace the `RepositoryEntityFrameworkBootstrap.Entities` static side channel, because five subsystems need the same entity census: EF model building, CRUD repos, filter constraints, security constraints, CrudApi/batch.

Per-package "what each explicit call must register" (today's implicit output) — abbreviated:
- `AddSencillaCore()` — dispatchers + middleware + `ISystemVariable`(scoped) + `SencillaApp` singleton; expose handler sweeps `RegisterFor<IEventHandlerBase<>>(asm)`, `RegisterFor<ICommandHandlerBase<>>(asm)`, `<,>`.
- `AddSencillaWeb(mvc)` — replace the feature-provider AppDomain scan with the entity registry or `AddCrudApiFor<T>()`.
- `AddSencillaRepositoryForEF(cfg)` + `AddEntitiesFrom(asm)`/`AddEntity<T>()` — per entity: repos + filters + model registration (kill the statics). **Fix C4 while here.**
- `AddSencillaSecurity()` — providers + `[AllowAccess]` matrix as a normal singleton + per-entity `AddSencillaSecurityForType`.
- `AddSencillaUsers()` / `AddI18n()` / `AddGeography()` / `AddSencillaFiles()` — self-register their own entities into the registry.
- `AddSencillaConfig()` (new) — note open-generic `IConfigProvider<>` is **never registered today** (scan skips generics): a latent gap to fix, not a regression.

## B.7 Breaking changes for consumers

1. `AddSencilla()` stops registering everything — **runtime** resolution errors, not compile errors.
2. `[assembly: AutoDiscovery]` in app assemblies becomes a no-op — services/handlers/entities silently vanish.
3. Entity infrastructure disappears per entity: no CRUD repos, empty `DynamicDbContext` model, while `[CrudApi]` controllers still appear (separate scan) → 500s at request time.
4. **Worst class — silent behavior regressions:** `FilterConstraintHandler<T>` and `SecurityConstraintHandler<T>` are wired per entity by scan; if missing, **reads succeed without constraints** (a security hole, not an exception).
5. Event/command handlers stop firing with no error.
6. Consumer `ITypeRegistrator`s never invoked (public API break if the contract is removed).
7. `SecurityAttributeDeclaration` ctor depends on the scan-registered `SecurityAttributeDiscoverer` singleton → container build failure.
8. `[Implement]`/lifetimes/`[DisableInjection]` become inert.
9. **Multiplicity drift:** scan uses `Add` (accumulates; `IEnumerable<T>` injection relies on it) vs the `RegisterFor` prototype's `TryAdd` (first-wins) → fewer handlers after migration unless you use `TryAddEnumerable`.
10. Concrete-class injection breaks (today every concrete class is resolvable as itself).
11. Docs invalidated wholesale.
12. Requires a major version bump + migration guide.

---

# Part C — Task 2: Split Core → Sencilla.Repository

**Goal:** move all repository abstractions out of `Sencilla.Core` into a new contracts package `Sencilla.Repository` (at `libs/repositories/Core`, mirroring `messaging/Core → Sencilla.Messaging`); implementations stay in `Sencilla.Repository.EntityFramework` etc. `Sencilla.Repository.SqlMapper` is dead — **remove from the sln, don't migrate.**

## C.1 What moves — and the coupling check

**`Repository/` (10 files) — moves in full.** Reverse check (critical): **nothing in `libs/core` outside `Repository/` references any repository type** (verified by grep). The cut is clean. The one non-entity/filter coupling is `SencillaAppExt` → `ISencillaApp` (in `Application/`) — fine as long as `ISencillaApp` stays in Core.

**`Filter/` (3 files) — moves to Sencilla.Repository.** `IFilter` is the query contract of `IReadRepository`; concrete `Filter` appears in `ICreateRepository.GetOrCreateAsync`. Inseparable from the contracts. Referenced in Core only by `Repository/*` and — **the one dirty edge** — `Entity/EntityBaseEvent.cs` (`IFilter? Filter`), which is why the events must move too.

**`Entity/` — split:**
- **MOVE (repository lifecycle artifacts):** the 8 `Entity*Event` classes (`EntityBaseEvent`, Creating/Created, Updating/Updated, Deleting/Deleted, Reading) + `GetOrCreateResult`. Raised **only** by repository implementations; `EntityBaseEvent : Event` (Event stays in Core, so Repository→Core is the allowed direction).
- **KEEP in Core (trait/marker interfaces + attrs):** `IEntity`/`IBaseEntity`/`IEntity<TKey>`/`IEntityGlobal`, all `IEntityXxx` traits, `IEntitySnapshot`, `IEntityAppendOnly(Track)`, `BusinessKeyAttribute`, `MainEntityAttribute`. Zero deps on Filter/Repository; `Config`/`Geography` declare entities without repos, so traits must sit **below** the new package.

## C.2 Reverse dependencies (package → contracts used)

| Project | Repo contracts | Filter | Events |
|---|---|---|---|
| Sencilla.Repository.EntityFramework | implements ALL + `IDbTransaction` + `GetOrCreateResult` | `IFilter`,`Filter`,`FilterProperty` | raises all 7 + handles Reading |
| Sencilla.Repository.HttpClient | Read/Create/Update/Delete/Remove | `IFilter` | — |
| Sencilla.Web | Read/Create/Update/Delete/Remove/AppendOnlyTrack | `Filter<T>`,`Filter`,`IFilter`,`FilterProperty` | — |
| Sencilla.Web.Batch | + **`IHide` (only consumer)** + `IDbTransaction` | `IFilter`,`Filter` | — |
| Sencilla.Extensions.EntityFrameworkCore | — | `Filter`, `IBaseEntity` | — |
| Sencilla.Component.Users | Read/Create | `Filter` | — |
| Sencilla.Component.Security | Read | — | handles Reading/Creating/Updating/Deleting |
| Sencilla.Component.I18n | Read/Create/Update | `Filter` | — |
| Sencilla.Component.Files | Read/Create/Update/Delete | `Filter` | — |
| Sencilla.Authentication | Read/Create/Update | — | — |
| tests: repositories/EF, files/Core, components/{Security,Users} | various | various | — |

`GlobalIdentityRepositoryExt` and `SencillaAppRepositoryExt` have **zero callers outside core** — move with no blast radius.

## C.3 Proposed layout

```
libs/repositories/Core/Sencilla.Repository.csproj   ← NEW (classlib, ProjectReference → core)
libs/repositories/Core/README.md                    ← NEW (Directory.Build.Common.props force-packs README → NU5019 if missing)
libs/repositories/Core/Bootstrap.cs                 ← NEW global usings: System, System.Linq.Expressions, Sencilla.Core
libs/repositories/Core/Repository/                   ← git mv libs/core/Repository/*  (10 files)
libs/repositories/Core/Filter/                       ← git mv libs/core/Filter/*       (3 files)
libs/repositories/Core/Events/                       ← git mv 8 event files + GetOrCreateResult.cs
```

Dependency graph (all arrows point down; **no cycles**):

```
Sencilla.Core  (no project refs; DI, Event, Command, ISencillaApp, Exceptions, Json, Entity traits)
     ▲
Sencilla.Repository  (I*Repository, IDbTransaction, IFilter/Filter, Entity*Events, GetOrCreateResult)
     ▲        ▲          ▲              ▲
Extensions.EF  Web   Repository.EF   Repository.HttpClient
                      ▲
              Web.Batch / Users / I18n / Files / Authentication / Security
```

## C.4 Namespace strategy — recommendation: **keep `namespace Sencilla.Core` on moved types (phase 1)**

1. Moved files carry **no `using` directives** (they rely on Core's global usings) and consumers import via `global using Sencilla.Core` — keep the namespace → moved files need **zero edits**, ~45 consumer files need **zero edits**; only csproj references change.
2. Precedent: `libs/web/Contract/IWebEntity.cs` already declares `Sencilla.Core` types from the Web assembly. Package boundary ≠ namespace here.
3. External consumers face only "add one PackageReference," not a repo-wide using rewrite.
4. Renaming can't buy binary compat anyway (C.6). If you want namespace purity, do it as a **separate phase-2 commit** (add `global using Sencilla.Repository;` to each consumer Bootstrap) so the move commit stays mechanically verifiable.

## C.5 Migration steps (in order)

1. Create `libs/repositories/Core/` — `Sencilla.Repository.csproj` (classlib + ProjectReference → `..\..\core\Sencilla.Core.csproj`), `README.md`, `Bootstrap.cs` with the three global usings.
2. `dotnet sln Sencilla.sln add libs/repositories/Core/Sencilla.Repository.csproj`.
3. `git mv libs/core/Repository libs/repositories/Core/Repository`.
4. `git mv libs/core/Filter libs/repositories/Core/Filter`.
5. `git mv` the 8 event files + `GetOrCreateResult.cs` → `libs/repositories/Core/Events/` (delete now-empty `Entity/Readable/`).
6. Add `<ProjectReference>` to the new package in every project that **directly uses** the moved types: extensions/EF, repositories/EF, repositories/HttpClient, web, webapi/Batch, components/Users, components/Security, components/I18n, files/Core, authentication/Core. (Tests get it transitively.) **No changes:** MinimalApi, ApiControllers, Config, Geography, Validation, files providers, messaging, scheduler, mappers, mobile, auth providers.
7. Remove `libs/repositories/SqlMapper` from `Sencilla.sln` (dead; would otherwise fail to compile).
8. Bump `<Version>` in `libs/Directory.Build.Common.props` 10.0.0 → **11.0.0**.
9. `dotnet build Sencilla.sln && dotnet test Sencilla.sln` — verify **all** projects (per the standing "verify before claiming fix" rule), not just the touched ones.
10. Docs sweep: `docs/core/{repositories,filtering,entities,README}.md`, `architecture.md`, `docs/repositories/*`, `index.md`, `getting-started.md`, `CLAUDE.md` links.
11. External consumers (Photoboost etc.): on upgrading to 11.0.0, add `<PackageReference Include="Sencilla.Repository" />` where repo contracts/Filter/events are used; no source edits if the namespace is kept.

## C.6 Risks

1. **Cycle trap #1 (the big one): `EntityBaseEvent.Filter : IFilter`.** If Filter/Repository move but events stay in Core → Core needs the new package → cycle. Mitigation baked in: **the 8 events move with the package.**
2. **Trap #2: `SencillaAppExt → ISencillaApp`** — fine as long as `ISencillaApp` stays in Core (don't move it).
3. **Trap #3: `Filter<TEntity> where TEntity : IBaseEntity`** — requires traits at/below the package; keeping traits in Core satisfies it.
4. **Trap #4:** `Sencilla.Extensions.EntityFrameworkCore` consumes `Filter` → it becomes a dependent of Sencilla.Repository (the "EF extensions" package is no longer repository-agnostic). Acceptable, or invert `ApplyFilter` (out of scope).
5. **Binary compat is unpreservable — no type forwarding.** Shims would need old `Sencilla.Core.dll` → new `Sencilla.Repository.dll` while the new dll references Core → circular assembly ref, unbuildable. Any prebuilt binary against Core ≤10.x touching moved types dies with `TypeLoadException` → **everything recompiles** (hence the major bump). Source compat preserved **only** under keep-namespace.
6. NuGet: lockstep versioning debuts Sencilla.Repository at 11.0.0. Core 11.0 is a *shrinking* package; floating-version consumers (`10.*`→`11.*`) break without the new ref. Release notes must say "add Sencilla.Repository."
7. **SqlMapper** implements the moved contracts → remove from sln or the whole build goes red.
8. **`IHideRepository` is implementation-orphaned** post-split (only impl was dead SqlMapper; only consumer is Web.Batch optional resolution). Move for completeness, but a deletion/EF-impl candidate.
9. **Global-usings gotcha:** moved files have no usings — forgetting the new `Bootstrap.cs` globals is the likely first-build failure (`Expression<>` unresolved).
10. **Packaging gotcha:** missing `README.md` in the new project → NU5019 pack failure.

---

# Part D — Optimization: performance, memory, API design

## D.1 Performance

- **Startup is all reflection, uncached, partly O(assemblies × types × registrators)** and enumerates the deferred queries **twice** (`Bootstrap.cs:71`). Materialize once. The real endgame: **source generators** for handler/entity registration (the repo already has messaging + scheduler SG projects). AOT-safe, near-zero startup cost. This is the natural terminus of Task 1.
- **Per-dispatch reflection on the command/event hot path** (`CommandDispatcher.cs:20`, `IServiceProviderExt.cs:36`). Each dispatch: `MethodInfo.Invoke` + a per-call string-join cache key + LINQ, even on cache hit; runs 1–2× per repository operation. **Fix:** compile a cached invoker delegate (`Expression.Lambda`/`Delegate.CreateDelegate` → `Func<object, TEvent, CancellationToken, Task>`) keyed by `(handlerType, eventType)`; precompute the injected-parameter plan once per method. Key the method cache on a `(Type, string, Type[])` struct comparer, not a built string.
- **Filter path does per-request reflection** (`ToIncludePath`) — cache resolved include paths per `(entityType, with)`.
- **Dynamic-LINQ string building per filter** is both the injection hole (C5) and a parse cost. Moving to typed `Expression<Func<T,bool>>` composition (you have `ExpressionEx`) removes both at once — the highest-leverage single change on this list.
- **`BusinessKeys.Match` uses `Expression.Constant`** → EF inlines values, defeating parameterization → plan-cache pollution on the supersede hot path. Wrap values so EF parameterizes; cache the predicate shape per type.

## D.2 Memory

- **`[UseCaching]` leak** (`CrudApiController.cs:28`) — identity-hash key means one dead `MemoryCache` entry per request for the whole expiration window. Fix `Filter` equality/hashing (also fixes correctness).
- **Process-wide statics leak across hosts** — `RepositoryEntityFrameworkBootstrap.Entities/Assemblies` + `DynamicDbContext._compiledModel`, never reset, unsynchronized. Move to options/instance state — the restructuring is the moment.
- **Repeated `IEnumerable` enumeration** in `CreateRepository` (5×) — `ToList` once at the boundary.
- `AsNoTracking().ToListAsync()` on reads is the right default — no tracked-entity retention. Keep it.

## D.3 API design

- **Optional `CancellationToken` before `params`** makes the include overloads unusable as intended (`IReadRepository.cs:29`). Put `params` last, token before it without a default, or split overloads.
- **Untyped aggregates** (`Task<object> GetSum/Max/Min`, `Task<double> GetAvarage`, plus the `GetAvarage` typo). Make them generic; `object` pushes casting onto every caller.
- **EF specifics leak into the neutral contract** (`Detach`/`ClearChangeTracker`/`JsonMergeAsync` on `IUpdateRepository`). Push to an EF-only capability interface — **this is the main blocker to `Sencilla.Repository` being genuinely provider-neutral**, so pair it with Task 2.
- **Freeze the spellings now** while making a breaking change: `Resolveable`, `Updator`/`Deletor`, `IEntityDeleteable`/`IEntityRemoveable`, `buidler`, `GetAvarage`.
- **Silent-return semantics** are a debugging hazard. Add opt-in strictness (`SendAsync` throwing on no handler in dev), and decide the event fan-out contract (isolate vs abort).

---

# Part E — Open decisions (the "grill list")

Decisions that must be made before coding; picking wrong now costs a rewrite.

**Auto-discovery removal**
1. **What replaces the entity registry, and does calling order become load-bearing?** Five subsystems need one shared entity census; `AddSencillaRepositoryForEF` must run *after* entities are registered or it builds an empty model. `AddEntity<T>()` vs `AddEntitiesFrom(asm)`? Deferred `Build()` phase (like `AuthenticationConfig`) to avoid order-dependence? — *Recommended: deferred build.*
2. **Is `RegisterFor<T>` (still reflection, assembly-bounded) the bar, or zero-startup-reflection via source generators?** You already have 2 SG projects.
3. **`Add` vs `TryAdd` multiplicity.** Scan uses `Add` (fan-out relies on it); the prototype uses `TryAdd` (drops the rest). Handler collections need `TryAddEnumerable` + a documented policy or handlers silently disappear.
4. **`ITypeRegistrator` fate.** Deleting it breaks Security's `SecurityAttributeDeclaration` ctor chain — redesign or repurpose to "invoked with explicit types."
5. **Handler sweep lifetime — Transient or Scoped?** Handlers touch scoped `DbContext`; Transient default risks captive dependencies.
6. **Non-`[AutoDiscovery]` AppDomain scans in scope?** `CrudApiControllerFeatureProvider`, `BatchEntityRegistry`, scheduler fallback are still "magic" after the marker mechanism is gone.
7. **Migration path:** hard cut (major bump, scan deleted) vs a transition release where `AddSencilla()` still scans but logs the explicit calls that would replace what it found (a "scan-to-code" generator makes consumer migration near-mechanical).

**Core → Sencilla.Repository split**
8. **Accept that entity lifecycle events are "repository" artifacts, not "core"?** They must move with Filter to avoid the cycle; Security + EF constraint handlers then reference the new package. (They're only ever raised by repositories — the judgment looks right.)
9. **Confirm traits stay in Core** (Config/Geography declare entities without repos; `Filter<T>`'s constraint needs traits at/below the package).
10. **Namespace:** keep `Sencilla.Core` phase-1 (recommended), rename later. Can't preserve binary compat either way.
11. **Ship as 11.0 with a migration note** ("add `Sencilla.Repository` PackageReference"); remove SqlMapper from the sln in the same PR.

**Quick wins independent of both tasks (safe to do first)**
- Fix **C4** (infinite recursion) and **C3** (typed command handler base interface) — one tiny PR, both are outright crashers/silent-nulls.
- Open issues for the two security items: **C5** (Dynamic-LINQ injection) and **C6** (delete events never published → security bypass on delete).
