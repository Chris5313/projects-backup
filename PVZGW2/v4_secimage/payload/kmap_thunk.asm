; ---------------------------------------------------------------------------
; kmap_thunk — thread-hijack landing pad, compiled INTO the payload image.
;
; The driver patches the target thread's KTRAP_FRAME::Rip to kmap_thunk.
; The thunk saves the full user context, calls the payload's real PE entry
; (base + AddressOfEntryPoint = _DllMainCRTStartup) as
; DllMain(base, DLL_PROCESS_ATTACH, NULL), restores the context and jumps
; back to the thread's original RIP. Zero executable scratch memory — the
; thunk lives in the RX .text of a kernel-mapped image section.
;
; Runtime values live in kmap_ctx (writable .data, filled by the driver):
;   +0x00  origRip  — thread's original RIP (written at hijack time)
;   +0x08  entry    — base + AddressOfEntryPoint (written at map time)
;   +0x10  dllBase  — image base, argument 1 for the entry
; ---------------------------------------------------------------------------
.code

PUBLIC kmap_thunk
PUBLIC kmap_ctx

.data
ALIGN 8
kmap_ctx DQ 0, 0, 0                 ; origRip | entry | dllBase

.code
kmap_thunk PROC
        ; ---- save full user context ----
        push    rax
        push    rcx
        push    rdx
        push    rbx
        push    rbp
        push    rsi
        push    rdi
        push    r8
        push    r9
        push    r10
        push    r11
        push    r12
        push    r13
        push    r14
        push    r15
        pushfq
        ; ---- align stack + shadow space (RSP % 16 == 0 before call) ----
        mov     rbp, rsp
        and     rsp, -16
        sub     rsp, 20h
        ; ---- entry(dllBase, DLL_PROCESS_ATTACH, NULL) ----
        mov     rcx, QWORD PTR [kmap_ctx+10h]
        mov     edx, 1
        xor     r8d, r8d
        call    QWORD PTR [kmap_ctx+8]
        ; ---- restore full user context ----
        mov     rsp, rbp
        popfq
        pop     r15
        pop     r14
        pop     r13
        pop     r12
        pop     r11
        pop     r10
        pop     r9
        pop     r8
        pop     rdi
        pop     rsi
        pop     rbp
        pop     rbx
        pop     rdx
        pop     rcx
        pop     rax
        jmp     QWORD PTR [kmap_ctx]
kmap_thunk ENDP

END
