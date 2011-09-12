def write_range(filename="D:/Development/OpenCV/SetVision/rgbrange.csv", step=20):
        import csv
	f = open(filename, "wb")
	writer = csv.DictWriter(f, ["R", "B", "G"], delimiter=';')
	writer.writeheader()
	for r in range(0, 255, step):
		for g in range(0, 255, step):
			for b in range(0, 255, step):
				writer.writerow({"R":r, "G":g, "B":b})
	f.close()
