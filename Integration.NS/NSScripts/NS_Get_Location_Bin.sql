SELECT 
	b.id AS NetsuiteBinInternalId,
	b.binnumber AS BinNumber
FROM
	bin b
WHERE
	location = @location