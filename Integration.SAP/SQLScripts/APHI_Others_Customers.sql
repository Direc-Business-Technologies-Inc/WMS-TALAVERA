SELECT DISTINCT
	 T0.CardCode
	,T0.CardName
FROM OCRD T0
WHERE
	CardType = 'C'
	AND ISNULL(T0.CardName, '') <> ''
