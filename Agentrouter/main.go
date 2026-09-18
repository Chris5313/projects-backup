package main

import (
	"fmt"
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
)

const (
	targetURL  = "https://agentrouter.org"
	listenPort = ":8318"
)

func main() {
	remote, err := url.Parse(targetURL)
	if err != nil {
		log.Fatalf("Failed to parse target URL: %v", err)
	}

	proxy := httputil.NewSingleHostReverseProxy(remote)

	originalDirector := proxy.Director
	proxy.Director = func(req *http.Request) {
		originalDirector(req)
		req.Host = remote.Host

		// Inject official Claude Code client headers required by AgentRouter WAF
		req.Header.Set("User-Agent", "claude-cli/2.1.158 (external, sdk-cli)")
		req.Header.Set("anthropic-version", "2023-06-01")
		req.Header.Set("anthropic-beta", "claude-code-20250219,interleaved-thinking-2025-05-14,effort-2025-11-24,redact-thinking-2026-02-12")
		req.Header.Set("anthropic-dangerous-direct-browser-access", "true")
		req.Header.Set("x-app", "cli")
		req.Header.Set("X-Stainless-Lang", "js")
		req.Header.Set("X-Stainless-OS", "Windows")
		req.Header.Set("X-Stainless-Arch", "x64")
		req.Header.Set("X-Stainless-Package-Version", "0.3.1")
	}

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		log.Printf("[Proxy] %s %s", r.Method, r.URL.Path)
		proxy.ServeHTTP(w, r)
	})

	fmt.Printf("AgentRouter Go proxy running on http://localhost%s forwarding to %s\n", listenPort, targetURL)
	if err := http.ListenAndServe(listenPort, nil); err != nil {
		log.Fatalf("Server failed: %v", err)
	}
}