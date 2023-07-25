.DATA
	SHUF1 DB    0,   0,   0,   0,   1,   0,   0,   0,   2,   0,   0,   0,   7,   0,   0,   0,   3,   0,   0,   0,   7,   0,   0,   0,   7,   0,   0,   0,   7,   0,   0,   0
	SHUFB DB    0,  15,  15,  15,   3,  15,  15,  15,   6,  15,  15,  15,   9,  15,  15,  15,   0,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15
	SHUFG DB    1,  15,  15,  15,   4,  15,  15,  15,   7,  15,  15,  15,  10,  15,  15,  15,   1,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15
	SHUFR DB    2,  15,  15,  15,   5,  15,  15,  15,   8,  15,  15,  15,  11,  15,  15,  15,   2,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15,  15
	QQQQQ DB  199,   1,   0,   0, 199,   1,   0,   0, 199,   1,   0,   0, 199,   1,   0,   0, 199,   1,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0
	SHUFF DB    2,   0,   0,   0,   3,   0,   0,   0,   4,   0,   0,   0,   7,   0,   0,   0,   7,   0,   0,   0,   7,   0,   0,   0,   7,   0,   0,   0,   7,   0,   0,   0
	SHUFO DB    0,   0,   0,   4,   4,   4,   8,   8,   8,   3,   3,   3,   3,   3,   3,   3,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0

.CODE
	passImageToAsm PROC
		PUSH RBX

		;;;;;;;;;;;; GET ARGUMENTS ;;;;;;;;;;;;;
		ADD RSP, 030H
		MOV RBX, 000000000FFFFFFFFH
		POP R10
		AND R10, RBX
		POP R11
		AND R11, RBX
		SUB RSP, 040H
		MOV R12, RCX
		MOV R13, RDX

		;;;; CALCULATE INPUT, OUTPUT STRIDE ;;;;
		MOV RBX, 0FFFFFFFFFFFFFFCH
		MOV RAX, 3
		INC R8
		MUL R8
		AND RAX, RBX
		MOV R14, RAX
		SUB R8, 2
		MOV RAX, 3
		MUL R8
		AND RAX, RBX
		MOV R15, RAX

		;;;;;;; INITIALIZE YMM REGISTERS ;;;;;;;
		VMOVUPS YMM15, YMMWORD PTR [SHUF1]
		VMOVUPS YMM14, YMMWORD PTR [SHUFB]
		VMOVUPS YMM13, YMMWORD PTR [SHUFG]
		VMOVUPS YMM12, YMMWORD PTR [SHUFR]
		VMOVUPS YMM11, YMMWORD PTR [QQQQQ]
		VMOVUPS YMM10, YMMWORD PTR [SHUFF]
		VMOVUPS YMM9,  YMMWORD PTR [SHUFO]
		VPXOR   YMM0, YMM0, YMM0
		VPXOR   YMM3, YMM3, YMM3
		VPXOR   YMM6, YMM0, YMM6

		;;;;;;; CALCULATE OUTPUT POINTER ;;;;;;;
		MOV RAX, R10
		MUL R15
		ADD R13, RAX

		;;;; CALCULATE INPUT BEGIN POINTER ;;;;;
		MOV RAX, R10
		MUL R14
		ADD RAX, R12
		MOV R10, RAX

		;;;;; CALCULATE INPUT END POINTER ;;;;;;
		MOV RAX, R11
		MUL R14
		ADD RAX, R12
		MOV R11, RAX

		;;;;;;;;; CALCULATE END OF ROW ;;;;;;;;;
		MOV RAX, 3
		MUL R8
		SUB RAX, 12
		ADD RAX, R10
		L1:
			;;;;; SET POINTER TO BEGIN OF ROW ;;;;;;
			MOV RBX, R10
			MOV RCX, R13
			L2:
				;;;;;;;;;;;; LOAD FIRST ROW ;;;;;;;;;;;;
				MOVUPS   XMM0, XMMWORD PTR [RBX]
				VPERMD   YMM0, YMM15, YMM0
				VPSHUFB  YMM2, YMM0,  YMM14
				VPSHUFB  YMM1, YMM0,  YMM13
				VPSHUFB  YMM0, YMM0,  YMM12

				;;;;;;;;;;; LOAD SECOND ROW ;;;;;;;;;;;;
				MOVUPS   XMM3, XMMWORD PTR [RBX + R14]
				VPERMD   YMM3, YMM15, YMM3
				VPSHUFB  YMM5, YMM3,  YMM14
				VPSHUFB  YMM4, YMM3,  YMM13
				VPSHUFB  YMM3, YMM3,  YMM12

				;;;;;;;;;;;; LOAD THIRD ROW ;;;;;;;;;;;;
				MOVUPS   XMM6, XMMWORD PTR [RBX + R14 * 2]
				VPERMD   YMM6, YMM15, YMM6
				VPSHUFB  YMM8, YMM6,  YMM14
				VPSHUFB  YMM7, YMM6,  YMM13
				VPSHUFB  YMM6, YMM6,  YMM12

				;;;;;;;;;;;;;;;;; SUM ;;;;;;;;;;;;;;;;;;
				VPADDD   YMM0, YMM0, YMM1
				VPADDD   YMM0, YMM0, YMM2
				VPADDD   YMM0, YMM0, YMM3
				VPADDD   YMM0, YMM0, YMM4
				VPADDD   YMM0, YMM0, YMM5
				VPADDD   YMM0, YMM0, YMM6
				VPADDD   YMM0, YMM0, YMM7
				VPADDD   YMM0, YMM0, YMM8

				;;;;;;;;;;;;; DIVIDE BY 9 ;;;;;;;;;;;;;;
				VPSLLD   YMM1, YMM0, 3
				VPSUBD   YMM0, YMM1, YMM0
				VPSLLD   YMM1, YMM0, 6
				VPADDD   YMM0, YMM0, YMM1
				VPADDD   YMM0, YMM0, YMM11
				VPSRLD   YMM0, YMM0, 12

				;;;;;;;;;;;;; APPLY FILTER ;;;;;;;;;;;;;
				VPERMD   YMM1, YMM10, YMM0
				VPSUBD   YMM0, YMM1,  YMM0

				;;;;;;;;;;;;;;;;; RELU ;;;;;;;;;;;;;;;;;
				VPXOR    YMM8, YMM8, YMM8
				VPMAXSD  YMM0, YMM0, YMM8

				;;;;;;;;;;;;;;;; STORE ;;;;;;;;;;;;;;;;;
				VPSHUFB  YMM0, YMM0, YMM9
				MOVLPD   QWORD PTR [RCX], XMM0
				PEXTRB   BYTE PTR [RCX + 8], XMM0, 8

				;;;;;;;;;; NEXT SLICE IN ROW ;;;;;;;;;;;
				ADD RBX, 9
				ADD RCX, 9
				CMP RBX, RAX
				JL L2
		;;;;;;;;;;;;;;; NEXT ROW ;;;;;;;;;;;;;;;
		ADD R10, R14
		ADD R13, R15
		ADD RAX, R14
		CMP R10, R11
		JNZ L1

		POP  RBX
		RET
	passImageToAsm ENDP

END