SELECT 
	b.id AS NetsuiteBinInternalId,
	b.binnumber AS BinNumber
FROM
	bin b
WHERE
	b.location IN ({locations})