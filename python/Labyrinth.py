#!/usr/bin/python3
# This was: #! /usr/bin/env python
# The labyrinthine abbey library in Python - October 3, 2013

import random
import sys

class RoomInfo:
	def __init__(self, level, room):
		self.levelNumber = level
		self.roomNumber = room
		#self.bookList = [] # Don't include this list in the __key, in order to keep the __key immutable (and the object hashable).

	# See http://stackoverflow.com/questions/2909106/python-whats-a-correct-and-good-way-to-implement-hash

	def __key(self):
		return (self.levelNumber, self.roomNumber)	# This is a tuple.

	def __eq__(x, y):
		return type(x) == type(y) and x.__key() == y.__key()

	def __hash__(self):
		return hash(self.__key())

	def ToString(self):
		return "(" + str(self.levelNumber) + ", " + str(self.roomNumber) + ")"

	#def Equals(self, otherRoom):	# TODO: Deprecate and delete this.
	#	return otherRoom != None and self.levelNumber == otherRoom.levelNumber and self.roomNumber == otherRoom.roomNumber

	#def GetHashCode(self):		# TODO: Deprecate and delete this.
	#	return self.levelNumber * 100 + self.roomNumber

	def GeneratePossibleNeighboursOnLevel(self, generator, newLevel):
		result = []

		if self.roomNumber == generator.numberOfRoomsPerLevel - 1:

			for i in range(0, generator.numberOfRoomsPerLevel - 1):
				result.append(RoomInfo(newLevel, i))
		else:
			result.append(RoomInfo(newLevel, (self.roomNumber + 1) % (generator.numberOfRoomsPerLevel - 1)))
			result.append(RoomInfo(newLevel, (self.roomNumber + generator.numberOfRoomsPerLevel - 2) % (generator.numberOfRoomsPerLevel - 1)))
			result.append(RoomInfo(newLevel, generator.numberOfRoomsPerLevel - 1))

		return result

	def GeneratePossibleNeighbours(self, generator):
		result = []

		if self.levelNumber > 0:
			result.extend(self.GeneratePossibleNeighboursOnLevel(generator, self.levelNumber - 1))

		if self.levelNumber < generator.numberOfLevels - 1:
			result.extend(self.GeneratePossibleNeighboursOnLevel(generator, self.levelNumber + 1))

		return result

class LabyrinthGenerator:
	def __init__(self, numberOfLevels, numberOfRoomsPerLevel):

		if numberOfLevels < 2 or numberOfRoomsPerLevel < 4: # or numberOfRoomsPerLevel > 100: # TODO: Delete the "> 100" condition when safe.
			raise Exception('LabyrinthGenerator.__init__(): Invalid parameter(s).')

		self.numberOfLevels = numberOfLevels
		self.numberOfRoomsPerLevel = numberOfRoomsPerLevel
		self.numberOfExtraConnections = 0
		self.numberOfExtraConnectionsAdded = 0
		self.extraConnections = [] #new List<KeyValuePair<RoomInfo, RoomInfo>>();
		self.rooms = [] #new List<RoomInfo>();
		self.roomLabels = {} #new Dictionary<RoomInfo, int>();
		self.connections = {} #new Dictionary<RoomInfo, List<RoomInfo>>();
		self.openList = [] #new List<RoomInfo>();
		#self.random = new Random();
		self.numberOfDifferentLabels = 0
		self.roomGoal = None
		self.booksInRooms = {} #new Dictionary<RoomInfo, string>();
		self.numberOfAttemptsToRefactor = 0
		self.maximumNumberOfAttemptsToRefactor = 100

	#def RoomListContainsRoom(self, roomList, room):	# TODO: Deprecate and delete this.
	#	return any(room.levelNumber == room2.levelNumber and room.roomNumber == room2.roomNumber for room2 in roomList)

	def FindConflictingConnections(self, room1, room2):
		# Test 0: Room labels ("blob numbers").

		#if (roomLabels[room1] == roomLabels[room2])
		#    return true;    // There is a conflict.

		# Test 1: Room 3 must not be connected to room 4.

		# 4  2
		#  \/
		#  /\
		# 1  3

		room3 = RoomInfo(room2.levelNumber, room1.roomNumber)
		room4 = RoomInfo(room1.levelNumber, room2.roomNumber)

		#if self.RoomListContainsRoom(self.connections[room3], room4):
		if room4 in self.connections[room3]:
			return True

		# Test 2: Room 3 must not be connected to room 1.

		# 3
		#  \
		#   1
		#  /
		# 2

		room3 = RoomInfo(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room1):
		#if self.connections.has_key(room3) and room1 in self.connections[room3]:
		if room3 in self.connections[room1]:
			return True

		# Test 3: Room 3 must not be connected to room 2.

		# 3
		#  \
		#   2
		#  /
		# 1

		room3 = RoomInfo(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room2):
		#if self.connections.has_key(room3) and room2 in self.connections[room3]:
		if room3 in self.connections[room2]:
			return True

		return False   # There is no conflict.

	def FindUnusedLabel(self):
		result = 0
		labels = self.roomLabels.values()

		while result in labels:
			result += 1

		return result

	def PropagateNewLabel(self, room, newLabel, addRoomsToOpenList):
		openListLocal = [] #new Stack<RoomInfo>();
		closedList = [] #new HashSet<RoomInfo>();

		openListLocal.append(room)

		while len(openListLocal) > 0:
			room = openListLocal.pop()
			self.roomLabels[room] = newLabel
			closedList.append(room)

			if addRoomsToOpenList and not (room in self.openList):
				self.openList.append(room)

			for room2 in self.connections[room]:

				#if (not self.RoomListContainsRoom(openListLocal, room2)) and (not self.RoomListContainsRoom(closedList, room2)):
				if (not room2 in openListLocal) and (not room2 in closedList):
					openListLocal.append(room2)

	def FindPossibleNeighboursWithDifferentLabels(self): #(out RoomInfo room1, out RoomInfo room2)
		openListLocal = list(room for room in self.rooms) #new List<RoomInfo>(rooms); # Clone the "rooms" list.

		while len(openListLocal) > 0:
			room1 = openListLocal[random.randint(0, len(openListLocal) - 1)]
			openListLocal.remove(room1)

			possibleNeighbours = room1.GeneratePossibleNeighbours(self)

			while len(possibleNeighbours) > 0:
				room2 = possibleNeighbours[random.randint(0, len(possibleNeighbours) - 1)]
				possibleNeighbours.remove(room2)

				if self.roomLabels[room1] != self.roomLabels[room2]:
					return (room1, room2)

		raise Exception("Unable to find possible neighbours with different labels.")

	def RemoveOneConnection(self, room1, room2):
		#self.connections[room1] = list(room for room in self.connections[room1] if room.levelNumber != room2.levelNumber or room.roomNumber != room2.roomNumber)
		self.connections[room1] = list(room for room in self.connections[room1] if room != room2)

	def RemoveBothConnection(self, room1, room2):
		self.RemoveOneConnection(room1, room2)
		self.RemoveOneConnection(room2, room1)

	def Refactor(self):
		# The print statement is replaced by the print() function in Python 3
		#print "Refactoring..." # This worked in Python 2
		print("Refactoring...")

		room1, room2 = self.FindPossibleNeighboursWithDifferentLabels()

		# Resolve the conflicts that are preventing a connection between room1 and room2.

		# Test 1: Room 3 must not be connected to room 4.

		# 4  2
		#  \/
		#  /\
		# 1  3

		room3 = RoomInfo(room2.levelNumber, room1.roomNumber)
		room4 = RoomInfo(room1.levelNumber, room2.roomNumber)

		#if self.RoomListContainsRoom(self.connections[room3], room4):
		if room4 in self.connections[room3]:
			print("Found a Type 1 conflict.")
			#self.connections[room3].remove(room4)
			#self.connections[room4].remove(room3)
			self.RemoveBothConnection(room3, room4)
			self.PropagateNewLabel(room3, self.FindUnusedLabel(), True)
			self.PropagateNewLabel(room4, self.FindUnusedLabel(), True)

		# Test 2: Room 3 must not be connected to room 1.

		# 3
		#  \
		#   1
		#  /
		# 2

		room3 = RoomInfo(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room1):
		#if self.connections.has_key(room3) and room1 in self.connections[room3]:
		if room3 in self.connections[room1]:
			print("Found a Type 2 conflict.")
			#self.connections[room1].remove(room3)
			#self.connections[room3].remove(room1)
			self.RemoveBothConnection(room1, room3)
			self.PropagateNewLabel(room3, self.FindUnusedLabel(), True)

		# Test 3: Room 3 must not be connected to room 2.

		# 3
		#  \
		#   2
		#  /
		# 1

		room3 = RoomInfo(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room2):
		#if self.connections.has_key(room3) and room2 in self.connections[room3]:
		if room3 in self.connections[room2]:
			print("Found a Type 3 conflict.")
			#self.connections[room2].remove(room3)
			#self.connections[room3].remove(room2)
			self.RemoveBothConnection(room2, room3)
			self.PropagateNewLabel(room3, self.FindUnusedLabel(), True)

		# Connect room1 and room2.
		self.PropagateNewLabel(room2, self.roomLabels[room1], False)
		self.connections[room1].append(room2)
		self.connections[room2].append(room1)

		self.numberOfDifferentLabels = len(set(self.roomLabels.values()))

	def FinalValidityCheck(self):
		self.PropagateNewLabel(RoomInfo(0, 0), self.FindUnusedLabel(), False)

		if len(set(self.roomLabels.values())) > 1:
			raise Exception("The labyrinth is in multiple blobs.")

		print("The labyrinth is a single blob.")

	#def AddExtraConnections(self):

	def Generate(self):
		label = 0

		self.numberOfDifferentLabels = self.numberOfLevels * self.numberOfRoomsPerLevel

		for l in range(0, self.numberOfLevels):

			for r in range(0, self.numberOfRoomsPerLevel):
				room = RoomInfo(l, r)

				self.rooms.append(room)
				self.roomLabels[room] = label
				label += 1
				self.connections[room] = [] #new List<RoomInfo>();
				self.openList.append(room)

		while self.numberOfDifferentLabels > 1:

			if len(self.openList) == 0:

				if self.numberOfAttemptsToRefactor >= self.maximumNumberOfAttemptsToRefactor:
					raise Exception("Attempted to refactor " + self.numberOfAttemptsToRefactor + " times; all failed.")

				self.numberOfAttemptsToRefactor += 1
				self.Refactor()

			room1 = self.openList[random.randint(0, len(self.openList) - 1)]
			possibleNeighbours = room1.GeneratePossibleNeighbours(self)
			room2 = None

			while room2 == None and len(possibleNeighbours) > 0:
				room2 = possibleNeighbours[random.randint(0, len(possibleNeighbours) - 1)]
				#print "room1:", room1.ToString(), "room2: ", room2.ToString()

				if self.roomLabels[room1] != self.roomLabels[room2] and not self.FindConflictingConnections(room1, room2):
					break

				possibleNeighbours.remove(room2)
				room2 = None

			if room2 == None:
				self.openList.remove(room1)
				continue

			# We have now chosen room1 and room2.
			self.connections[room1].append(room2)
			self.connections[room2].append(room1)

			# Join the two "blobs" to which the two rooms belong, by modifying room labels.
			label1 = self.roomLabels[room1]
			label2 = self.roomLabels[room2]
			minLabel = min(label1, label2)
			maxLabel = max(label1, label2)

			for room in self.rooms:

				if self.roomLabels[room] == maxLabel:
					self.roomLabels[room] = minLabel

			self.numberOfDifferentLabels -= 1

		#if self.numberOfExtraConnections > 0:
		#	self.AddExtraConnections()

		self.Report()
		self.PrintLongestPath()		# This sets roomGoal.
		self.PlaceBooksInRooms()	# This uses roomGoal.

	def Report(self):

		for room in self.rooms:

			for otherRoom in self.connections[room]:
				print(room.ToString(), "to", otherRoom.ToString())

		#if (numberOfExtraConnections > 0)

		#	foreach (var extraConnection in extraConnections)
		#    		Console.WriteLine("Extra connection added: {0} to {1}.", extraConnection.Key, extraConnection.Value);

		#	Console.WriteLine("{0} extra connection(s) requested; {1} added.", numberOfExtraConnections, numberOfExtraConnectionsAdded);

		if self.numberOfAttemptsToRefactor > 0:
			print("The labyrinth was refactored", self.numberOfAttemptsToRefactor, "time(s).")

		self.FinalValidityCheck()

	def FindShortestPathBetweenRooms(self, room, roomGoalLocal):
		openListLocal = [room] #new Queue<RoomInfo>();
		paths = {room: [room]} #new Dictionary<RoomInfo, List<RoomInfo>>();

		#openListLocal.Enqueue(room);
		#paths[room] = new List<RoomInfo>() { room };

		#if room.Equals(roomGoalLocal):
		if room == roomGoalLocal:
			return paths[room]

		while len(openListLocal) > 0:
			room = openListLocal.pop(0)

			for room2 in self.connections[room]:

				if not (room2 in paths.keys()):	# paths.Keys is essentially the union of openListLocal and closedList.
					openListLocal.append(room2)
					paths[room2] = list(r for r in paths[room])
					paths[room2].append(room2)

					#if room2.Equals(roomGoalLocal):
					if room2 == roomGoalLocal:
						return paths[room2]

		# Here, room is the last room to be dequeued (and thus the last room to be enqueued).
		return paths[room]

	def FindLongestPathFromRoom(self, room):
		return self.FindShortestPathBetweenRooms(room, None)

	def PrintLongestPath(self):
		path1 = self.FindLongestPathFromRoom(RoomInfo(self.numberOfLevels - 1, self.numberOfRoomsPerLevel - 1))
		longestPath = self.FindLongestPathFromRoom(path1[len(path1) - 1])

		print()
		#Console.WriteLine("The longest path contains {0} rooms:", longestPath.Count);
		#Console.WriteLine(string.Join(" to ", longestPath));
		print("The longest path contains", len(longestPath), "rooms.")

		self.roomGoal = longestPath[len(longestPath) - 1]

		pathFromOriginToGoal = self.FindShortestPathBetweenRooms(RoomInfo(0, 0), self.roomGoal)

		print()
		#Console.WriteLine("Aristotle's Second Book of the Poetics is in Room {0}.", roomGoal);
		#Console.WriteLine();
		#Console.WriteLine("The path from Room (0, 0) to Room {0} contains {1} rooms:", roomGoal, pathFromOriginToGoal.Count);
		#Console.WriteLine(string.Join(" to ", pathFromOriginToGoal));
		print("The path from Room (0, 0) to the goal contains", len(pathFromOriginToGoal), "rooms.")

	def PlaceBooksInRooms(self):
		books = [
			"The First Book of the Poetics of Aristotle",
			"The Iliad by Homer",
			"The Odyssey by Homer",
			"The Republic by Plato",
			"Categories by Aristotle",
			"Physics by Aristotle",
			"Nicomachean Ethics by Aristotle",
			"The Aeneid by Virgil",
			"The Old Testament in Hebrew",
			"The New Testament in Greek",
			"Strong's Hebrew Dictionary",
			"Strong's Greek Dictionary"
		]
		openListLocal = list(room for room in self.rooms)
		numBooksPlaced = 1

		self.booksInRooms[self.roomGoal] = "The Second Book of the Poetics of Aristotle"
		openListLocal.remove(self.roomGoal)

		while numBooksPlaced * 3 < len(self.rooms) and len(books) > 0:
			roomHashCode = openListLocal[random.randint(0, len(openListLocal) - 1)]
			book = books[random.randint(0, len(books) - 1)]

			openListLocal.remove(roomHashCode)
			books.remove(book)
			self.booksInRooms[roomHashCode] = book
			numBooksPlaced += 1

		#print "The books have been placed."

	def ReportProximityToJorge(self, room, JorgesRoom):
		path = self.FindShortestPathBetweenRooms(room, JorgesRoom)
		distance = len(path) - 1

		if distance == 0:
			print("* You and the Venerable Jorge are in the same room! *")
			print("'Good evening, Venerable Jorge.'")
		elif distance <= 2:
			print("The Venerable Jorge is very near.")
		elif distance <= 4:
			print("The Venerable Jorge is near.")

	def ConstructJorgesPath(self, JorgesRoom):
		#RoomInfo JorgesGoal;

		# ThAW 2013/10/04 : There appears to be no do...while loop in Python.
		while True:
			JorgesGoal = self.rooms[random.randint(0, len(self.rooms) - 1)]

			#if not JorgesGoal.Equals(JorgesRoom):
			if JorgesGoal != JorgesRoom:
				break

		return self.FindShortestPathBetweenRooms(JorgesRoom, JorgesGoal)

	def NavigateLabyrinth(self):
		roomsVisited = [] #new HashSet<RoomInfo>();
		room = RoomInfo(0, 0)

		#Console.WriteLine("Selecting a room for Jorge out of {0} rooms.", rooms.Count);

		JorgesRoom = self.rooms[random.randint(0, len(self.rooms) - 1)]
		JorgesPath = self.ConstructJorgesPath(JorgesRoom)
		JorgesPathIndex = 0

		while True:
			#roomsVisited.Add(room);

			#if not self.RoomListContainsRoom(roomsVisited, room):
			if not room in roomsVisited:
				roomsVisited.append(room)

			print()
			print("You are now in room " + room.ToString() + ".")
			#Console.WriteLine("The Venerable Jorge is now in room {0}.", JorgesRoom);
			#Console.WriteLine("Jorge's destination is room {0}", JorgesPath[JorgesPath.Count - 1]);

			self.ReportProximityToJorge(room, JorgesRoom)

			#if self.booksInRooms.has_key(room): # Python 2
			if room in self.booksInRooms.keys(): # has_key() was removed from the dictionary class in Python 3
				print("You have found the book '" + self.booksInRooms[room] + "'.")

			#if room.Equals(self.roomGoal):
			if room == self.roomGoal:
				print("**** Congratulations!  You have reached the goal! ****")

			neighbouringRooms = self.connections[room]

			print("Possible moves:")

			for i in range(0, len(neighbouringRooms)):
				neighbouringRoom = neighbouringRooms[i]
				s = "  " + str(i) + ". " + neighbouringRoom.ToString()	# "s" is for "string".

				#if self.RoomListContainsRoom(roomsVisited, neighbouringRoom):
				if neighbouringRoom in roomsVisited:
					s = s + " Visited"

				print(s)

			#print "This is 'foo'."
			# See https://stackoverflow.com/questions/1093322/how-do-i-check-what-version-of-python-is-running-my-script
			# assert sys.version_info >= (2,5)
			# print(sys.version_info)
			# print(sys.version_info[0])
			# print(sys.version_info.major)
			
			if (sys.version_info.major == 3):
				print("Python 3")
				inputStr = input("Your move (or (h)elp or (q)uit): ") # Python 2's raw_input() is called "input()" in Python 3
			elif (sys.version_info.major == 2):
				print("Python 2")
				inputStr = raw_input("Your move (or (h)elp or (q)uit): ") # Python 2
			else:
				raise Exception('LabyrinthGenerator.NavigateLabyrinth(): Invalid version of Python: ' + str(sys.version_info))

			if (inputStr == ""):
				print("The input is empty.")
			elif inputStr == "h":
				pathToGoal = self.FindShortestPathBetweenRooms(room, self.roomGoal)
				pathAsString = ""
				separator = ""

				for roomInPath in pathToGoal:
					pathAsString = pathAsString + separator + roomInPath.ToString()
					separator = " to "

				print("Path to goal: " + pathAsString + ".")
			elif inputStr == "q":
				break
			else:

				try:
					inputInt = int(inputStr)

					if inputInt < 0 or inputInt >= len(neighbouringRooms):
						print("The input is out of range.")
					else:
						room = neighbouringRooms[inputInt]
						self.ReportProximityToJorge(room, JorgesRoom)
				except (NameError, SyntaxError, ValueError):
					print("The input was not recognized.")

			# Jorge's move.
			JorgesPathIndex += 1

			while JorgesPathIndex >= len(JorgesPath): # ThAW 2013/09/23 : This "while" used to be an "if", but it crashed once.
				JorgesPath = self.ConstructJorgesPath(JorgesRoom)
				JorgesPathIndex = 1

			JorgesRoom = JorgesPath[JorgesPathIndex]

# Non-class code:

#def InputInt():
#	result = 0
#	while True:
#		try:
#			result = int(input('Enter an integer: '))
#			break
#		except (NameError, SyntaxError, ValueError):
#			print 'That was not an integer.'
#			continue
#	return result

print("Creating the generator...")
generator = LabyrinthGenerator(15, 7)

#r = RoomInfo(13, 0)
#neighs = r.GeneratePossibleNeighbours(generator)
#print list(neigh.ToString() for neigh in neighs)

#r = RoomInfo(13, 6)
#neighs = r.GeneratePossibleNeighbours(generator)
#print list(neigh.ToString() for neigh in neighs)

#r = RoomInfo(0, 0)
#neighs = r.GeneratePossibleNeighbours(generator)
#print list(neigh.ToString() for neigh in neighs)

#r = RoomInfo(0, 6)
#neighs = r.GeneratePossibleNeighbours(generator)
#print list(neigh.ToString() for neigh in neighs)

#r = RoomInfo(14, 5)
#neighs = r.GeneratePossibleNeighbours(generator)
#print list(neigh.ToString() for neigh in neighs)

#r = RoomInfo(14, 6)
#neighs = r.GeneratePossibleNeighbours(generator)
#print list(neigh.ToString() for neigh in neighs)

#r2 = RoomInfo(13, 0)
#r3 = RoomInfo(13, 6)
#dict = {r: 'Foo', r2: 'Bar'}
#print dict[r], dict[r2]
#print dict.has_key(r), dict.has_key(r2), dict.has_key(r3)
#r4 = RoomInfo(14, 6)
#print "dict has key r4:", dict.has_key(r4)
#print "r4 in list [r, r2, r3]:", r4 in [r, r2, r3]

#i = InputInt()
#print 'InputInt() returned ', i

random.seed()

#for i in range (0, 5):
#	print random.randint(0, 99) # 0 <= n <= 99

#l = [2, 3, 5, 7]
#l2 = list(x for x in l)
#l[1] = 13
#print l, l2

#tu = (7, 13)
#e1, e2 = tu
#print e1
#print e2

#s = set([2, 3, 5, 3, 5, 7])
#print s
#print len(s)

#print list(i for i in [1, 2, 3, 4, 5] if i != 3)

generator.Generate()

#print "From (0, 0) to (14, 6):", list(room.ToString() for room in generator.FindShortestPathBetweenRooms(RoomInfo(0, 0), RoomInfo(14,6)))
#print "From (0, 0) to None:", list(room.ToString() for room in generator.FindShortestPathBetweenRooms(RoomInfo(0, 0), None))

generator.NavigateLabyrinth()