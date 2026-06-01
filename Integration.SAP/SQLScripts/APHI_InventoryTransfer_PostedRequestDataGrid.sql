SELECT T0.DocEntry
	,T0.DocNum
	,T0.DocDate
	,T1.WhsName [FromWhsName]
	,T2.WhsName [ToWhsName]
	,T0.U_Remarks [Remarks]
	,ISNULL(T0.U_PrepBy, '-') [PreparedBy]
FROM OWTR T0
LEFT JOIN OWHS T1 ON T0.Filler = T1.WhsCode
LEFT JOIN OWHS T2 ON T0.ToWhsCode = T2.WhsCode
WHERE 
	T0.DocStatus = 'O'
	AND T0.CANCELED = 'N'
	AND T0.Comments LIKE '%WMS%'