#!/usr/bin/env python3
"""Talk to the ida-pro-mcp plugin over Streamable HTTP (127.0.0.1:13337/mcp).

Usage:
  ida_rpc.py list                        # list tool names
  ida_rpc.py call <toolname> [json-args] # call a tool, args default {}
"""
import json
import os
import sys
import urllib.request

URL = os.environ.get("IDA_URL", "http://127.0.0.1:13337/mcp")
SESSION = {"sid": None}


def _headers():
    h = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
    if SESSION["sid"]:
        h["Mcp-Session-Id"] = SESSION["sid"]
    return h


def _post(payload):
    req = urllib.request.Request(URL, data=json.dumps(payload).encode(), headers=_headers())
    r = urllib.request.urlopen(req, timeout=120)
    sid = r.headers.get("Mcp-Session-Id")
    if sid:
        SESSION["sid"] = sid
    body = r.read().decode(errors="replace")
    if not body.strip():
        return None
    ctype = r.headers.get("Content-Type", "")
    if "text/event-stream" in ctype:
        for line in body.splitlines():
            if line.startswith("data:"):
                return json.loads(line[5:].strip())
        return None
    return json.loads(body)


def init():
    resp = _post({"jsonrpc": "2.0", "method": "initialize", "params": {
        "protocolVersion": "2025-06-18",
        "capabilities": {},
        "clientInfo": {"name": "buffy", "version": "1.0"},
    }, "id": 0})
    _post({"jsonrpc": "2.0", "method": "notifications/initialized"})
    return resp


def rpc(method, params=None, id_=1):
    resp = _post({"jsonrpc": "2.0", "method": method, "params": params or {}, "id": id_})
    if not resp:
        raise RuntimeError("empty response (timeout?)")
    if "error" in resp:
        raise RuntimeError(json.dumps(resp["error"]))
    return resp["result"]


def tools_list():
    return rpc("tools/list")["tools"]


def tools_call(name, args):
    result = rpc("tools/call", {"name": name, "arguments": args}, id_=2)
    texts = []
    for item in result.get("content", []):
        if item.get("type") == "text":
            texts.append(item["text"])
        else:
            texts.append(json.dumps(item)[:2000])
    return "\n".join(texts)


def main():
    init()
    if len(sys.argv) < 2 or sys.argv[1] == "list":
        for t in tools_list():
            print(t["name"], "-", (t.get("description") or "").splitlines()[0][:100])
        return
    if sys.argv[1] == "call":
        name = sys.argv[2]
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        print(tools_call(name, args))


if __name__ == "__main__":
    main()
