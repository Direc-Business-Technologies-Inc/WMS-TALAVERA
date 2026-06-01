SELECT 
	 T0.DocEntry
	,T0.DocNum
	,T0.DocDate
	,T0.DocDueDate
	,T0.CardCode
	,C0.CardName
	,P0.Name [SupplierContactPerson]
	,ISNULL(T0.Comments, '') [Remarks]
	,CASE 
		WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
		WHEN T0.DocStatus = 'C' THEN 'Closed'
	ELSE 'Open'
	End as DocStatus
FROM OPOR AS T0
INNER JOIN OCRD AS C0 ON T0.CardCode = C0.CardCode
LEFT JOIN OCPR AS P0 ON T0.CntctCode = P0.CntctCode
WHERE 
	T0.DocStatus = 'O'