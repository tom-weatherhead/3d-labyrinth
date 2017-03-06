#!/usr/bin/ruby
# The labyrinthine abbey library in Ruby - October 14, 2013

require 'set'

class RoomInfo
	attr_accessor :levelNumber
	attr_accessor :roomNumber

	# Create the object
	def initialize(levelNumber, roomNumber)
		@levelNumber = levelNumber
		@roomNumber = roomNumber
	end

	# To string
	def to_s()
		#return "(#{@levelNumber}, #{@roomNumber})"
		"(#{@levelNumber}, #{@roomNumber})"
	end

	def ==(other)
		# See http://stackoverflow.com/questions/7156955/whats-the-difference-between-equal-eql-and
		#return @levelNumber == other.levelNumber && @roomNumber == other.roomNumber
		!other.nil?() && @levelNumber == other.levelNumber && @roomNumber == other.roomNumber # Should we use "and" instead of "&&"?
	end

	#def eql?(other)
	#	return self == other
	#end

	alias eql? ==

	def hash
		# See http://www.ruby-doc.org/core-2.0.0/Hash.html
		# The "return" keyword appears to be optional.
		@levelNumber.hash ^ @roomNumber.hash # XOR
	end

	def generatePossibleNeighboursOnLevel(generator, newLevel)
		result = []

		if @roomNumber == generator.numberOfRoomsPerLevel - 1

			(0 .. generator.numberOfRoomsPerLevel - 2).each do |i|
				result.push(RoomInfo.new(newLevel, i))
			end
		else
			result.push(RoomInfo.new(newLevel, (@roomNumber + 1) % (generator.numberOfRoomsPerLevel - 1)))
			result.push(RoomInfo.new(newLevel, (@roomNumber + generator.numberOfRoomsPerLevel - 2) % (generator.numberOfRoomsPerLevel - 1)))
			result.push(RoomInfo.new(newLevel, generator.numberOfRoomsPerLevel - 1))
		end

		result
	end

	def generatePossibleNeighbours(generator)
		result = []

		if @levelNumber > 0
			result.concat(generatePossibleNeighboursOnLevel(generator, @levelNumber - 1))
		end

		if @levelNumber < generator.numberOfLevels - 1
			result.concat(generatePossibleNeighboursOnLevel(generator, @levelNumber + 1))
		end

		return result
	end
end

class LabyrinthGenerator
	attr_accessor :numberOfLevels
	attr_accessor :numberOfRoomsPerLevel

	# Create the object
	def initialize(numberOfLevels, numberOfRoomsPerLevel)

		#if numberOfLevels < 2 or numberOfRoomsPerLevel < 4:
		#	raise Exception('LabyrinthGenerator.__init__(): Invalid parameter(s).')

		@numberOfLevels = numberOfLevels
		@numberOfRoomsPerLevel = numberOfRoomsPerLevel
		@numberOfExtraConnections = 0
		@numberOfExtraConnectionsAdded = 0
		@extraConnections = [] #new List<KeyValuePair<RoomInfo, RoomInfo>>();
		@rooms = [] #new List<RoomInfo>();
		@roomLabels = {} #new Dictionary<RoomInfo, int>();
		@connections = {} #new Dictionary<RoomInfo, List<RoomInfo>>();
		@openList = [] #new List<RoomInfo>();
		#self.random = new Random();
		@numberOfDifferentLabels = 0
		@roomGoal = nil #None
		@booksInRooms = {} #new Dictionary<RoomInfo, string>();
		@numberOfAttemptsToRefactor = 0
		@maximumNumberOfAttemptsToRefactor = 100
	end

	def findConflictingConnections(room1, room2)
		# Test 0: Room labels ("blob numbers").

		#if (roomLabels[room1] == roomLabels[room2])
		#    return true;    // There is a conflict.

		# Test 1: Room 3 must not be connected to room 4.

		# 4  2
		#  \/
		#  /\
		# 1  3

		room3 = RoomInfo.new(room2.levelNumber, room1.roomNumber)
		room4 = RoomInfo.new(room1.levelNumber, room2.roomNumber)

		#if self.RoomListContainsRoom(self.connections[room3], room4):
		if @connections[room3].include?(room4)
			return true
		end

		# Test 2: Room 3 must not be connected to room 1.

		# 3
		#  \
		#   1
		#  /
		# 2

		room3 = RoomInfo.new(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room1):
		#if self.connections.has_key(room3) and room1 in self.connections[room3]:
		if @connections[room1].include?(room3)
			return true
		end

		# Test 3: Room 3 must not be connected to room 2.

		# 3
		#  \
		#   2
		#  /
		# 1

		room3 = RoomInfo.new(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room2):
		#if self.connections.has_key(room3) and room2 in self.connections[room3]:
		if @connections[room2].include?(room3)
			return true
		end

		false   # There is no conflict.
	end

	def findUnusedLabel
		result = 0
		labels = @roomLabels.values

		while labels.include?(result)
			result += 1
		end

		result
	end

	def propagateNewLabel(room, newLabel, addRoomsToOpenList)
		openListLocal = [] #new Stack<RoomInfo>();
		closedList = [] #new HashSet<RoomInfo>();

		openListLocal.push(room)

		while openListLocal.length > 0
			room = openListLocal.pop()
			@roomLabels[room] = newLabel
			closedList.push(room)

			if addRoomsToOpenList && !@openList.include?(room)
				@openList.push(room)
			end

			@connections[room].each do |room2|

				#if (not self.RoomListContainsRoom(openListLocal, room2)) and (not self.RoomListContainsRoom(closedList, room2)):
				if !openListLocal.include?(room2) && !closedList.include?(room2)
					openListLocal.push(room2)
				end
			end
		end
	end

	def findPossibleNeighboursWithDifferentLabels
		openListLocal = Array.new(@rooms) # Clone the "rooms" list.

		while openListLocal.length > 0
			room1 = openListLocal.delete_at(rand(openListLocal.length))
			possibleNeighbours = room1.generatePossibleNeighbours(self);

			while possibleNeighbours.length > 0
				room2 = possibleNeighbours.delete_at(rand(possibleNeighbours.length))

				if @roomLabels[room1] != @roomLabels[room2]
					return [room1, room2]	# Is there such a thing as a tuple in Ruby?
				end
			end
		end

		raise "Unable to find possible neighbours with different labels."
	end

	def removeOneConnection(room1, room2)
		#self.connections[room1] = list(room for room in self.connections[room1] if room.levelNumber != room2.levelNumber or room.roomNumber != room2.roomNumber)
		@connections[room1].delete(room2) # = list(room for room in self.connections[room1] if room != room2)
	end

	def removeBothConnection(room1, room2)
		removeOneConnection(room1, room2)
		removeOneConnection(room2, room1)
	end

	def refactor
		puts "Refactoring..."

		#[room1, room2] = findPossibleNeighboursWithDifferentLabels() # Can we assign to members of a list in Ruby?
		l = findPossibleNeighboursWithDifferentLabels()
		room1 = l[0]
		room2 = l[1]

		# Resolve the conflicts that are preventing a connection between room1 and room2.

		# Test 1: Room 3 must not be connected to room 4.

		# 4  2
		#  \/
		#  /\
		# 1  3

		room3 = RoomInfo.new(room2.levelNumber, room1.roomNumber)
		room4 = RoomInfo.new(room1.levelNumber, room2.roomNumber)

		#if self.RoomListContainsRoom(self.connections[room3], room4):
		if @connections[room3].include?(room4)
			puts "Found a Type 1 conflict."
			#self.connections[room3].remove(room4)
			#self.connections[room4].remove(room3)
			removeBothConnection(room3, room4)
			propagateNewLabel(room3, findUnusedLabel(), true)
			propagateNewLabel(room4, findUnusedLabel(), true)
		end

		# Test 2: Room 3 must not be connected to room 1.

		# 3
		#  \
		#   1
		#  /
		# 2

		room3 = RoomInfo.new(2 * room1.levelNumber - room2.levelNumber, room2.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room1):
		#if self.connections.has_key(room3) and room1 in self.connections[room3]:
		if @connections[room1].include?(room3)
			puts "Found a Type 2 conflict."
			#self.connections[room1].remove(room3)
			#self.connections[room3].remove(room1)
			removeBothConnection(room1, room3)
			propagateNewLabel(room3, findUnusedLabel(), true)
		end

		# Test 3: Room 3 must not be connected to room 2.

		# 3
		#  \
		#   2
		#  /
		# 1

		room3 = RoomInfo.new(2 * room2.levelNumber - room1.levelNumber, room1.roomNumber)

		#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room2):
		#if self.connections.has_key(room3) and room2 in self.connections[room3]:
		if @connections[room2].include?(room3)
			puts "Found a Type 3 conflict."
			#self.connections[room2].remove(room3)
			#self.connections[room3].remove(room2)
			removeBothConnection(room2, room3)
			propagateNewLabel(room3, findUnusedLabel(), true)
		end

		# Connect room1 and room2.
		propagateNewLabel(room2, @roomLabels[room1], false)
		@connections[room1].push(room2)
		@connections[room2].push(room1)

		@numberOfDifferentLabels = @roomLabels.values().to_set().length()
	end

	def finalValidityCheck
		propagateNewLabel(RoomInfo.new(0, 0), findUnusedLabel(), false)

		if @roomLabels.values().to_set().length() > 1
			raise "The labyrinth is in multiple blobs."
		end

		puts "The labyrinth is a single blob."
	end

	#def AddExtraConnections(self):

	def generate()
		label = 0

		@numberOfDifferentLabels = @numberOfLevels * @numberOfRoomsPerLevel

		(0 .. @numberOfLevels - 1).each do |l|

			(0 .. @numberOfRoomsPerLevel - 1).each do |r|
				room = RoomInfo.new(l, r);

				@rooms.push(room)
				@roomLabels[room] = label
				#puts "Label for #{room.to_s} is #{label}"
				label += 1
				@connections[room] = [] #new List<RoomInfo>();
				@openList.push(room)
			end
		end

		while @numberOfDifferentLabels > 1

			if @openList.length() == 0

				if @numberOfAttemptsToRefactor >= @maximumNumberOfAttemptsToRefactor
					raise "Attempted to refactor #{@numberOfAttemptsToRefactor} times; all failed."
				end

				@numberOfAttemptsToRefactor += 1
				refactor()
			end

			room1 = @openList[rand(@openList.length())]
			possibleNeighbours = room1.generatePossibleNeighbours(self)
			room2 = nil

			while room2.nil? && possibleNeighbours.length() > 0
				room2 = possibleNeighbours[rand(possibleNeighbours.length())]
				#print "room1:", room1.ToString(), "room2: ", room2.ToString()

				if @roomLabels[room1] != @roomLabels[room2] && !findConflictingConnections(room1, room2)
					break
				end

				possibleNeighbours.delete(room2)
				room2 = nil
			end

			if room2.nil?
				@openList.delete(room1)
				next #continue
			end

			# We have now chosen room1 and room2.
			@connections[room1].push(room2)
			@connections[room2].push(room1)

			# Join the two "blobs" to which the two rooms belong, by modifying room labels.
			label1 = @roomLabels[room1]
			label2 = @roomLabels[room2]
			minLabel = [label1, label2].min
			maxLabel = [label1, label2].max

			@rooms.each do |room|

				if @roomLabels[room] == maxLabel
					@roomLabels[room] = minLabel
				end
			end

			@numberOfDifferentLabels -= 1
		end

		#if self.numberOfExtraConnections > 0:
		#	self.AddExtraConnections()

		report()
		printLongestPath()		# This sets roomGoal.
		placeBooksInRooms()		# This uses roomGoal.
	end

	def report

		# @rooms.each do |room|
		for room in @rooms

			# @connections[room].each do |otherRoom|
			for otherRoom in @connections[room]
				puts "#{room.to_s} to #{otherRoom.to_s}"
			end
		end

		#if (numberOfExtraConnections > 0)

		#	foreach (var extraConnection in extraConnections)
		#    		Console.WriteLine("Extra connection added: {0} to {1}.", extraConnection.Key, extraConnection.Value);

		#	Console.WriteLine("{0} extra connection(s) requested; {1} added.", numberOfExtraConnections, numberOfExtraConnectionsAdded);

		if @numberOfAttemptsToRefactor > 0
			puts "The labyrinth was refactored #{@numberOfAttemptsToRefactor} time(s)."
		end

		finalValidityCheck()
	end

	def findShortestPathBetweenRooms(room, roomGoalLocal)
		openListLocal = [room] #new Queue<RoomInfo>();
		paths = {} #{room: [room]} #new Dictionary<RoomInfo, List<RoomInfo>>();

		#openListLocal.Enqueue(room);
		paths[room] = [room] #new List<RoomInfo>() { room };

		#if room.Equals(roomGoalLocal):
		if room == roomGoalLocal
			return paths[room]
		end

		while openListLocal.length() > 0
			room = openListLocal.shift()

			@connections[room].each do |room2|

				if !paths.key?(room2)	# paths.Keys is essentially the union of openListLocal and closedList.
					openListLocal.push(room2)
					paths[room2] = Array.new(paths[room])
					paths[room2].push(room2)

					#if room2.Equals(roomGoalLocal):
					if room2 == roomGoalLocal
						return paths[room2]
					end
				end
			end
		end

		# Here, room is the last room to be dequeued (and thus the last room to be enqueued).
		return paths[room]
	end

	def findLongestPathFromRoom(room)
		return findShortestPathBetweenRooms(room, nil)
	end

	def printLongestPath()
		path1 = findLongestPathFromRoom(RoomInfo.new(@numberOfLevels - 1, @numberOfRoomsPerLevel - 1))
		longestPath = findLongestPathFromRoom(path1[path1.length() - 1])

		puts ""
		#Console.WriteLine("The longest path contains {0} rooms:", longestPath.Count);
		#Console.WriteLine(string.Join(" to ", longestPath));
		puts "The longest path contains #{longestPath.length()} rooms."

		@roomGoal = longestPath[longestPath.length() - 1]

		pathFromOriginToGoal = findShortestPathBetweenRooms(RoomInfo.new(0, 0), @roomGoal)

		print ""
		#Console.WriteLine("Aristotle's Second Book of the Poetics is in Room {0}.", roomGoal);
		#Console.WriteLine();
		#Console.WriteLine("The path from Room (0, 0) to Room {0} contains {1} rooms:", roomGoal, pathFromOriginToGoal.Count);
		#Console.WriteLine(string.Join(" to ", pathFromOriginToGoal));
		print "The path from Room (0, 0) to the goal contains #{pathFromOriginToGoal.length()} rooms."
	end

	def placeBooksInRooms()
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
		openListLocal = Array.new(@rooms)
		numBooksPlaced = 1

		@booksInRooms[@roomGoal] = "The Second Book of the Poetics of Aristotle"
		openListLocal.delete(@roomGoal)

		while numBooksPlaced * 3 < @rooms.length() && books.length() > 0
			room = openListLocal.delete_at(rand(openListLocal.length()))
			book = books.delete_at(rand(books.length()))

			@booksInRooms[room] = book
			numBooksPlaced += 1
		end
	end

	def reportProximityToJorge(room, jorgesRoom)	# Note the lowercase "j"
		path = findShortestPathBetweenRooms(room, jorgesRoom)
		distance = path.length() - 1

		if distance == 0
			puts "* You and the Venerable Jorge are in the same room! *"
			puts "'Good evening, Venerable Jorge.'"
		elsif distance <= 2
			puts "The Venerable Jorge is very near."
		elsif distance <= 4
			puts "The Venerable Jorge is near."
		end
	end

	def constructJorgesPath(jorgesRoom)
		jorgesGoal = nil

		# See http://www.tutorialspoint.com/ruby/ruby_loops.htm
		begin
			jorgesGoal = @rooms[rand(@rooms.length())]
		end while jorgesGoal == jorgesRoom

		return findShortestPathBetweenRooms(jorgesRoom, jorgesGoal)
	end

	def navigateLabyrinth()
		roomsVisited = Set.new #[] #new HashSet<RoomInfo>();
		room = RoomInfo.new(0, 0)

		#Console.WriteLine("Selecting a room for Jorge out of {0} rooms.", rooms.Count);

		jorgesRoom = @rooms[rand(@rooms.length())]
		jorgesPath = constructJorgesPath(jorgesRoom)
		jorgesPathIndex = 0

		while true
			#roomsVisited.Add(room);

			#if not self.RoomListContainsRoom(roomsVisited, room):
			#if not room in roomsVisited:
			roomsVisited.add(room)

			puts ""
			puts "You are now in room #{room.to_s()}."
			#puts "The Venerable Jorge is now in room #{jorgesRoom.to_s()}."
			#puts "Jorge's destination is room #{jorgesPath[jorgesPath.length() - 1].to_s()}."

			reportProximityToJorge(room, jorgesRoom)

			if @booksInRooms.key?(room)
				puts "You have found the book '#{@booksInRooms[room]}'."
			end

			#if room.Equals(self.roomGoal):
			if room == @roomGoal
				puts "**** Congratulations!  You have reached the goal! ****"
			end

			neighbouringRooms = @connections[room]

			puts "Possible moves:"

			(0 .. neighbouringRooms.length() - 1).each do |i|
				neighbouringRoom = neighbouringRooms[i]
				s = "  #{i}. #{neighbouringRoom.to_s()}"	# "s" is for "string".

				#if self.RoomListContainsRoom(roomsVisited, neighbouringRoom):
				if roomsVisited.include?(neighbouringRoom)
					s = s + " Visited"
				end

				puts s
			end

			#print "This is 'foo'."
			print "Your move (or (h)elp or (q)uit): "	# TODO: Do not print a newline here (perhaps by using "print"?)
			inputStr = gets.chomp #raw_input("Your move (or (h)elp or (q)uit): ")
			#puts "inputStr is '#{inputStr}'."

			# String comparison: See http://www.techotopia.com/index.php/Ruby_String_Concatenation_and_Comparison

			if inputStr.eql?("")
				puts "The input is empty."
			elsif inputStr.eql?("h")
				pathToGoal = findShortestPathBetweenRooms(room, @roomGoal)
				pathAsString = pathToGoal.map { |r| r.to_s() }.join(" to ")
				puts "Path to goal: #{pathAsString}."
			elsif inputStr.eql?("q")
				break
			else

				begin
					inputInt = Integer(inputStr)

					if inputInt < 0 or inputInt >= neighbouringRooms.length()
						puts "The input is out of range."
					else
						room = neighbouringRooms[inputInt]
						reportProximityToJorge(room, jorgesRoom)
					end
				rescue
					puts "The input was not recognized."
				end
			end

			# Jorge's move.
			jorgesPathIndex += 1

			while jorgesPathIndex >= jorgesPath.length() # ThAW 2013/09/23 : This "while" used to be an "if", but it crashed once.
				jorgesPath = constructJorgesPath(jorgesRoom)
				jorgesPathIndex = 1
			end

			jorgesRoom = jorgesPath[jorgesPathIndex]
		end
	end
end

if __FILE__ == $0
	puts "Hello world!"

	array = [1, 2, 3]
	array << [4, 5]
	array << 6
	puts "Array: #{array.join(", ")}"
	popValue = array.pop()
	puts "popValue (6): #{popValue}"
	shiftValue = array.shift()
	puts "shiftValue (1): #{shiftValue}"
	puts "Array: #{array.join(", ")}"

	room = RoomInfo.new(14, 6)
	puts room.to_s()

	room2 = RoomInfo.new(14, 6)
	result1 = room == room2
	result2 = room.eql?(room2)
	puts "Result of ==: #{result1}"
	puts "Result of eql?: #{result2}"

	hash = {}
	hash[room] = 7
	hashValue = hash[room2]
	puts "Hash value (7): #{hashValue}"

	array = [2, 3, 5, 7]
	arrayContains3 = array.include?(3)
	arrayContains4 = array.include?(4)
	puts "Array contains 3 (true): #{arrayContains3}"
	puts "Array contains 4 (false): #{arrayContains4}"

	generator = LabyrinthGenerator.new(15, 7)
	room3 = RoomInfo.new(7, 0)
	room3Neighs = room3.generatePossibleNeighbours(generator).map { |r| r.to_s() }.join(" and ")
	puts "room3 neighbours: #{room3Neighs}"
	room4 = RoomInfo.new(7, 6)
	room4Neighs = room4.generatePossibleNeighbours(generator).map { |r| r.to_s() }.join(", ")
	puts "room4 neighbours: #{room4Neighs}"

	arrayOfRooms = [room2, room3, room4]
	arrayContainsFirstRoom = arrayOfRooms.include?(RoomInfo.new(0, 0))
	arrayContainsLastRoom = arrayOfRooms.include?(RoomInfo.new(14, 6))
	puts "Array contains (0, 0) (false): #{arrayContainsFirstRoom}"
	puts "Array contains (14, 6) (true): #{arrayContainsLastRoom}"

	generator.generate()
	generator.navigateLabyrinth()
end