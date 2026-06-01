select
	 T0.DocEntry
	,T0.DocNum
    ,T0.DocDate
	,T0.U_PrepBy [PreparedBy]
	,T2.Code [TransTypeCode]
    ,T2.Name [TransTypeName]
    ,T3.AcctCode
    ,T3.AcctName
	,T1.Status
from ODRF T0
INNER JOIN OWDD T1 ON T0.DocEntry = T1.DraftEntry AND T0.ObjType = T1.ObjType
LEFT JOIN [@TRANSACTION_TYPE] T2 ON T2.Code = T0.U_TransType
LEFT JOIN OACT T3 ON T3.FormatCode = T2.U_AcctCode
WHERE 
	T0.ObjType = 60
	AND T0.Comments LIKE '%WMS%' 