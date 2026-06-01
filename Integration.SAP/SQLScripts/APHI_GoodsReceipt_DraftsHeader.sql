SELECT
	  T0.DocEntry
	, T0.DocNum
    , T0.DocDate
	, T2.Code [TransTypeCode]
    , T2.Name [TransTypeName]
    , T3.AcctCode
    , T3.AcctName
    , T0.U_PURNo [PurNo]
    , T0.U_TransType [TransType]
    , T0.U_WARNo [WarNo]
    , T0.U_BpCode [CardCode]
    , T0.U_BPName [CardName]
    , T0.U_PrepBy [PreparedBy]
    , T0.U_AppBy [ApprovedBy]
    , T0.U_Remarks [DocRemarks]
    , T0.U_NotedBy [NotedBy]
    , T0.U_RecBy [ReceivedBy]
    , T0.U_Desig [Designation]
FROM ODRF T0
INNER JOIN OWDD T1 ON T0.DocEntry = T1.DraftEntry AND T0.ObjType = T1.ObjType
LEFT JOIN [@TRANSACTION_TYPE] T2 ON T2.Code = T0.U_TransType
LEFT JOIN OACT T3 ON T3.FormatCode = T2.U_AcctCode
WHERE 
	T0.ObjType = @ObjType 
	AND T0.DocEntry = @DocEntry