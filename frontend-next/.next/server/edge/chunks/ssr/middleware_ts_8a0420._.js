(globalThis.TURBOPACK = globalThis.TURBOPACK || []).push(["chunks/ssr/middleware_ts_8a0420._.js", {

"[project]/middleware.ts [middleware] (ecmascript)": (({ r: __turbopack_require__, f: __turbopack_module_context__, i: __turbopack_import__, s: __turbopack_esm__, v: __turbopack_export_value__, n: __turbopack_export_namespace__, c: __turbopack_cache__, M: __turbopack_modules__, l: __turbopack_load__, j: __turbopack_dynamic__, P: __turbopack_resolve_absolute_path__, U: __turbopack_relative_url__, R: __turbopack_resolve_module_id_path__, g: global, __dirname, x: __turbopack_external_require__, y: __turbopack_external_import__, k: __turbopack_refresh__ }) => (() => {
"use strict";

__turbopack_esm__({
    "config": ()=>config,
    "middleware": ()=>middleware
});
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$esm$2f$api$2f$server$2e$js__$5b$middleware$5d$__$28$ecmascript$29$__$3c$module__evaluation$3e$__ = __turbopack_import__("[project]/node_modules/next/dist/esm/api/server.js [middleware] (ecmascript) <module evaluation>");
var __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$esm$2f$server$2f$web$2f$spec$2d$extension$2f$response$2e$js__$5b$middleware$5d$__$28$ecmascript$29$__ = __turbopack_import__("[project]/node_modules/next/dist/esm/server/web/spec-extension/response.js [middleware] (ecmascript)");
"__TURBOPACK__ecmascript__hoisting__location__";
;
function middleware(request) {
    const token = request.cookies.get('token')?.value;
    const requestHeaders = new Headers(request.headers);
    // Inject the Bearer token if it exists in the HttpOnly cookie
    if (token) {
        requestHeaders.set('Authorization', `Bearer ${token}`);
    }
    // Route API calls to the Gateway
    if (request.nextUrl.pathname.startsWith('/api') && !request.nextUrl.pathname.startsWith('/api/internal')) {
        const gatewayUrl = new URL(request.nextUrl.pathname + request.nextUrl.search, 'http://localhost:5000');
        return __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$esm$2f$server$2f$web$2f$spec$2d$extension$2f$response$2e$js__$5b$middleware$5d$__$28$ecmascript$29$__["NextResponse"].rewrite(gatewayUrl, {
            request: {
                headers: requestHeaders
            }
        });
    }
    // Route SignalR hubs to the Gateway
    if (request.nextUrl.pathname.startsWith('/hubs')) {
        const gatewayUrl = new URL(request.nextUrl.pathname + request.nextUrl.search, 'http://localhost:5000');
        return __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$esm$2f$server$2f$web$2f$spec$2d$extension$2f$response$2e$js__$5b$middleware$5d$__$28$ecmascript$29$__["NextResponse"].rewrite(gatewayUrl, {
            request: {
                headers: requestHeaders
            }
        });
    }
    // Protect internal UI routes if no token exists
    const isApi = request.nextUrl.pathname.startsWith('/api');
    const isAuth = request.nextUrl.pathname.startsWith('/login');
    const isHubs = request.nextUrl.pathname.startsWith('/hubs');
    if (!token && !isApi && !isAuth && !isHubs) {
        return __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$esm$2f$server$2f$web$2f$spec$2d$extension$2f$response$2e$js__$5b$middleware$5d$__$28$ecmascript$29$__["NextResponse"].redirect(new URL('/login', request.url));
    }
    // Pass along the request
    return __TURBOPACK__imported__module__$5b$project$5d2f$node_modules$2f$next$2f$dist$2f$esm$2f$server$2f$web$2f$spec$2d$extension$2f$response$2e$js__$5b$middleware$5d$__$28$ecmascript$29$__["NextResponse"].next({
        request: {
            headers: requestHeaders
        }
    });
}
const config = {
    // Apply middleware to all requests except static files and next internals
    matcher: [
        '/((?!_next/static|_next/image|favicon.ico).*)'
    ]
};

})()),
}]);

//# sourceMappingURL=middleware_ts_8a0420._.js.map