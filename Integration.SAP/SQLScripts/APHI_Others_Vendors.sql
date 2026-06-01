SELECT DISTINCT
	 T0.CardCode
	,T0.CardName
FROM OCRD T0
WHERE 
	CardType = 'S'
	AND ISNULL(T0.CardName, '') <> ''