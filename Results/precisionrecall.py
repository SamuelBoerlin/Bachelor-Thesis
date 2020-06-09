import matplotlib.pyplot as plt
import pandas as pd
import numpy as np
import os
import math
import sys, getopt

opts = []
try:
	opts, _ = getopt.getopt(sys.argv[1:], "f:", ["folder="])
except getopt.GetoptError:
	pass
if len(opts) == 0:
	print 'precisionrecall.py -f <folder>'
	sys.exit(2)
folder = opts[0][1]

print("Evaluating '" + folder + "'")

plt.figure(num="ClusterD2+Color Precision & Recall")

categories = {}

categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "ants") for num in range(0,29+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "crabs") for num in range(30,59+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "hands") for num in range(60,79+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "humans") for num in range(80,108+1)]})
categories["b102_m.obj"] = "humans"
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "octopuses") for num in range(109,133+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "pliers") for num in range(134,153+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "snakes") for num in range(154,178+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "spectacles") for num in range(179,203+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "spiders") for num in range(204,234+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "teddybears") for num in range(235,254+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "airplanes") for num in range(255,280+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "birds") for num in range(281,301+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "chairs") for num in range(302,324+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "cups") for num in range(325,349+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "dinosaurs") for num in range(350,368+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "dolphins") for num in range(369,380+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "fishes") for num in range(381,403+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "fourlimbs") for num in range(404,434+1)]})
categories.update({key: value for (key, value) in [("b" + str(num) + ".obj", "tables") for num in range(435,456+1)]})
	
def interpolate(xlist, ylist, x):
	n = len(xlist)
	s = int(x * (n - 1))
	sx = xlist[s]
	sy = ylist[s]
	if s == n - 1:
		return sy
	else:	
		ex = xlist[s+1]
		ey = ylist[s+1]
	xrange = ex - sx
	yrange = ey - sy
	fraction = x - s * 1.0 / (n - 1)
	return sy + yrange / xrange * fraction

files = [f for f in os.listdir(folder) if os.path.isfile(os.path.join(folder, f)) and f.endswith(".csv")]

# upsample by interpolation
numsamples = 1000

avgx = np.zeros(numsamples)
avgy = np.zeros(numsamples)

for f in files:
	modelName = f[:-4]
	category = categories[modelName]
	print("Computing results for: " + modelName + " (" + category + ")")
	data = pd.read_csv(os.path.join(folder, f))

	n = len(data["ObjectName"])
	
	objectOrder = {}
	recall = {}
	inClassRanking = {}
	inClassRank = 0
	for j in range(0, n):
		objectName = data["ObjectName"][j]
		if categories[objectName] == category:
			inClassRank = inClassRank + 1
			inClassRanking[objectName] = inClassRank
			objectOrder[inClassRank] = objectName
		
	for objectName in inClassRanking:
		recall[objectName] = (inClassRanking[objectName] - 1) * 1.0 / (inClassRank - 1)

	precision = {}
	for objectName in recall:
		foundRank = False
		for rank in range(0, n):
			if(data["ObjectName"][rank] == objectName):
				precision[objectName] = inClassRanking[objectName] * 1.0 / (rank + 1)
				foundRank = True
				break
		if not foundRank:
			print("Failed to find rank for " + objectName)
	
	y = []
	x = []
	for j in range(0, inClassRank):
		x.append(recall[objectOrder[j + 1]])
		y.append(precision[objectOrder[j + 1]])
	
	ix = np.zeros(numsamples)
	iy = np.zeros(numsamples)
	for j in range(0, numsamples):
		ix[j] = j * 1.0 / (numsamples - 1)
		iy[j] = (interpolate(x, y, ix[j]))
	
	avgx = avgx + ix
	avgy = avgy + iy
	
avgx = avgx / len(files)
avgy = avgy / len(files)

numpoints = 21

plotx = np.zeros(numpoints)
ploty = np.zeros(numpoints)

for j in range(0, numpoints):
	plotx[j] = j * 1.0 / (numpoints - 1)
	ploty[j] = interpolate(avgx, avgy, plotx[j])

plt.gca().set_xlim([0, 1])
plt.gca().set_ylim([0, 1])
plt.gca().xaxis.set_ticks_position("both")
plt.gca().yaxis.set_ticks_position("both")
plt.xticks([v / 10.0 for v in range(0, 11)])
plt.yticks([v / 10.0 for v in range(0, 11)])
plt.gca().set_aspect('equal', adjustable='box')
plt.plot(plotx, ploty, "-o")
plt.xlabel('Recall')
plt.ylabel('Precision')
plt.show()