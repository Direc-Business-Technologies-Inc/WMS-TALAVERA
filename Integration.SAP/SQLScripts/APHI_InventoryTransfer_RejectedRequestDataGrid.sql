SELECT
	 T0.DocEntry
	,T0.DocNum
    ,T0.DocDate
    ,T0.U_Remarks [Remarks]
	,T0.U_PrepBy [PreparedBy]
	,T1.WhsName [FromWhsName]
	,T2.WhsName [ToWhsName]
FROM ODRF T0
INNER JOIN OWDD T3 ON T0.DocEntry = T3.DraftEntry
LEFT JOIN OWHS T1 ON T1.WhsCode = T0.Filler
LEFT JOIN OWHS T2 ON T2.WhsCode = T0.ToWhsCode
WHERE 
	T0.ObjType = 67
	AND T3.Status = 'N'
	AND T0.Comments LIKE '%WMS%' 