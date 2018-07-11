# The labyrinthine abbey library in Perl - October 9, 2013

package LabyrinthGenerator;

use strict;
use warnings FATAL => qw(all);

use RoomInfo;

sub new {
	# See page 338.
	my $invocant = shift;
	my $numberOfLevels = shift;
	my $numberOfRoomsPerLevel = shift;
	($numberOfLevels >= 2 && $numberOfRoomsPerLevel >= 4) or die "The labyrinth dimensions are too small.";
	my $self = bless({}, ref $invocant || $invocant);
	$self->{numberOfLevels} = $numberOfLevels;
	$self->{numberOfRoomsPerLevel} = $numberOfRoomsPerLevel;
	$self->{numberOfExtraConnections} = 0;
	$self->{numberOfExtraConnectionsAdded} = 0;
	$self->{extraConnections} = [];
	$self->{rooms} = [];
	$self->{roomLabels} = {};
	$self->{connections} = {};
	$self->{openList} = [];
	$self->{numberOfDifferentLabels} = 0;
	#$self->{roomGoal} = ;
	$self->{booksInRooms} = {};
	$self->{numberOfAttemptsToRefactor} = 0;
	$self->{maximumNumberOfAttemptsToRefactor} = 100;
	return $self;
}

sub roomListContainsRoom {
	my $self = shift;
	my $roomList = shift;
	my $room = shift;
	#print "roomList: $roomList\n";
	#print "room: " . $room->toString() . "\n";

	for my $room2 (@$roomList) {
		#print "room2: " . $room2->toString() . "\n";

		#if ($room->equals($room2)) {
		if ($room == $room2) {
			return 1;
		}
	}

	return 0;
}

sub findConflictingConnections {
	my $self = shift;
	my $room1 = shift;
	my $room2 = shift;

    # Test 0: Room labels ("blob numbers").

    #if (roomLabels[room1] == roomLabels[room2])
    #    return true;    // There is a conflict.

    # Test 1: Room 3 must not be connected to room 4.

    # 4  2
    #  \/
    #  /\
    # 1  3

	my $room3 = RoomInfo->new($room2->{levelNumber}, $room1->{roomNumber});
	my $room4 = RoomInfo->new($room1->{levelNumber}, $room2->{roomNumber});

	#if room4 in self.connections[room3]:
	#if ($self->roomListContainsRoom($self->{connections}{$room3->toString()}, $room4)) {
	if ($self->roomListContainsRoom($self->{connections}{$room3}, $room4)) {
		return 1;
	}

	# Test 2: Room 3 must not be connected to room 1.

	# 3
	#  \
	#   1
	#  /
	# 2

	$room3 = RoomInfo->new(2 * $room1->{levelNumber} - $room2->{levelNumber}, $room2->{roomNumber});

	#if self.connections.has_key(room3) and room1 in self.connections[room3]:
	#if (exists $self->{connections}{$room3->toString()} && $self->roomListContainsRoom($self->{connections}{$room3->toString()}, $room1)) {
	#if ($self->roomListContainsRoom($self->{connections}{$room1->toString()}, $room3)) {
	if ($self->roomListContainsRoom($self->{connections}{$room1}, $room3)) {
		return 1;
	}

	# Test 3: Room 3 must not be connected to room 2.

	# 3
	#  \
	#   2
	#  /
	# 1

	$room3 = RoomInfo->new(2 * $room2->{levelNumber} - $room1->{levelNumber}, $room1->{roomNumber});

	#if self.connections.has_key(room3) and room2 in self.connections[room3]:
	#if (exists $self->{connections}{$room3->toString()} && $self->roomListContainsRoom($self->{connections}{$room3->toString()}, $room2)) {
	#if ($self->roomListContainsRoom($self->{connections}{$room2->toString()}, $room3)) {
	if ($self->roomListContainsRoom($self->{connections}{$room2}, $room3)) {
		return 1;
	}

	return 0;	# There is no conflict.
}

sub labelIsUsed {
	my $self = shift;
	my $label = shift;
	#my $roomLabels = $self->{roomLabels};

	# 2013/10/30 : Is there a "hash iterator" that needs to be reset (because of the "return") before we begin the "while"?  Yes; see page 704.
	# To reset the hash's iterator, evaluate "keys" or "values" of the hash, e.g.:
	#my @hashKeys = keys %{$self->{roomLabels}};

	#while (($roomTemp, $labelTemp) = each %$roomLabels) {
	#while ((my $roomTemp, my $labelTemp) = each %{$self->{roomLabels}}) { # 2013/10/29 : The version of the loop in these three lines was buggy.
	#	return 1 if $label == $labelTemp;
	#}

	#for my $key (keys %{$self->{roomLabels}}) {
	#	return 1 if $label == $self->{roomLabels}{$key};
	#}

	for (values %{$self->{roomLabels}}) {
		return 1 if $label == $_;
	}

	return 0;
}

sub findUnusedLabel {
	my $self = shift;
   	my $result = 0;

	while ($self->labelIsUsed($result)) {
		++$result;
	}

	#print "findUnusedLabel: Returning $result\n";
	return $result;
}

sub propagateNewLabel { #(self, room, newLabel, addRoomsToOpenList):
	my $self = shift;
	my $room = shift;
	my $newLabel = shift;
	my $addRoomsToOpenList = shift;
	my $openListLocal = []; #new Stack<RoomInfo>();
	my $closedList = []; #new HashSet<RoomInfo>();

	push @$openListLocal, $room;

	while (@$openListLocal) {
	#while ((scalar @$openListLocal) > 0) {
		my $roomFromOpenList = pop @$openListLocal;
		#$self->{roomLabels}{$roomFromOpenList->toString()} = $newLabel;
		$self->{roomLabels}{$roomFromOpenList} = $newLabel;
		push @$closedList, $roomFromOpenList;

		if ($addRoomsToOpenList && !$self->roomListContainsRoom($self->{openList}, $roomFromOpenList)) {
			push @{$self->{openList}}, $roomFromOpenList;
		}

		#my $roomConnections = $self->{connections}{$roomFromOpenList->toString()};
		my $roomConnections = $self->{connections}{$roomFromOpenList};

		for my $room2 (@$roomConnections) {

			#if (not room2 in openListLocal) and (not room2 in closedList):
			if ((!$self->roomListContainsRoom($openListLocal, $room2)) && (!$self->roomListContainsRoom($closedList, $room2))) {
				#print "Pushing " . $room2->toString() . "\n";
				push @$openListLocal, $room2;
			}
		}
	}

	#print "Propagate: Number of unique labels is " . $self->numberOfUniqueRoomLabels() . "\n";
}

#sub cloneRoomList {
#	my $self = shift;
#	my $roomList = shift;
#	my $result = [];
#
#	for my $room (@$roomList) {
#		push @$result, $room;
#	}
#
#	return $result;
#}

sub findPossibleNeighboursWithDifferentLabels { #(self): #(out RoomInfo room1, out RoomInfo room2)
	my $self = shift;
	#my $openListLocal = $self->cloneRoomList($self->{rooms}); # list(room for room in self.rooms) #new List<RoomInfo>(rooms); # Clone the "rooms" list.
	my @openListLocalHelper = @{$self->{rooms}}; # Copy the rooms list by value
	my $openListLocal = \@openListLocalHelper;

	while (@$openListLocal) { # This causes a bug; the labyrinth becomes multiple blobs.
	#while ((scalar @$openListLocal) > 0) {
		#my $room1 = $$openListLocal[int(rand(scalar @$openListLocal))]; # rand calls srand; see page 768.
		#openListLocal.remove(room1)
		#my $room1 = splice @$openListLocal, int(rand(scalar @$openListLocal)), 1; # Remove a random element from the list.  See page 793.
		my $room1 = splice @$openListLocal, int(rand @$openListLocal), 1; # Remove a random element from the list.  See page 793.

		#print "findPossibleNeighboursWithDifferentLabels: room1 == " . $room1->toString() . "\n";;
		my @possibleNeighbours = $room1->generatePossibleNeighbours($self->{numberOfLevels}, $self->{numberOfRoomsPerLevel});

		#while ((scalar @possibleNeighbours) > 0) {
		while (@possibleNeighbours) {
			#room2 = possibleNeighbours[random.randint(0, len(possibleNeighbours) - 1)]
			#possibleNeighbours.remove(room2)
			#my $room2 = splice @possibleNeighbours, int(rand(scalar @possibleNeighbours)), 1;
			my $room2 = splice @possibleNeighbours, int(rand @possibleNeighbours), 1;

			#if ($self->{roomLabels}{$room1->toString()} != $self->{roomLabels}{$room2->toString()}) {
			if ($self->{roomLabels}{$room1} != $self->{roomLabels}{$room2}) {
				return ($room1, $room2);
			}
		}
	}

	#$self->printRoomLabels();
	die "Unable to find possible neighbours with different labels.";
}

sub removeOneConnection { #(self, room1, room2):
	my $self = shift;
	my $room1 = shift;
	my $room2 = shift;
	#my $room1Connections = $self->{connections}{$room1->toString()};
	my $room1Connections = $self->{connections}{$room1};

	for my $i (0 .. $#$room1Connections) {

		#if ($room2->equals($$room1Connections[$i])) {
		if ($room2 == $$room1Connections[$i]) {
			#print "Removing connection to " . $room2->toString() . ".\n";
			splice @$room1Connections, $i, 1;
			return;
		}
	}
}

sub removeBothConnection { #(self, room1, room2):
	my $self = shift;
	my $room1 = shift;
	my $room2 = shift;

	#print "Removing the connection between " . $room1->toString() . " and " . $room2->toString() . "...\n";
	$self->removeOneConnection($room1, $room2);
	$self->removeOneConnection($room2, $room1);
}

sub numberOfUniqueRoomLabels {
	my $self = shift;
	my @listOfLabels;

	#while ((my $roomTemp, my $labelTemp) = each %{$self->{roomLabels}}) {
	#	push @listOfLabels, $labelTemp;
	#}

	for my $room (@{$self->{rooms}}) {
		#push @listOfLabels, $self->{roomLabels}{$room->toString()};
		push @listOfLabels, $self->{roomLabels}{$room};
	}

	my %hashTemp;

	@hashTemp{@listOfLabels} = (0) x (scalar @listOfLabels);
	return scalar keys %hashTemp;
}

sub refactor { #(self):
	my $self = shift;

	#print "Refactoring...\n";

	(my $room1, my $room2) = $self->findPossibleNeighboursWithDifferentLabels();

	# Resolve the conflicts that are preventing a connection between room1 and room2.

	# Test 1: Room 3 must not be connected to room 4.

	# 4  2
	#  \/
	#  /\
	# 1  3

	my $room3 = RoomInfo->new($room2->{levelNumber}, $room1->{roomNumber});
	my $room4 = RoomInfo->new($room1->{levelNumber}, $room2->{roomNumber});

	#if room4 in self.connections[room3]:
	#if ($self->roomListContainsRoom($self->{connections}{$room3->toString()}, $room4)) {
	if ($self->roomListContainsRoom($self->{connections}{$room3}, $room4)) {
		#print "Found a Type 1 conflict.\n";
		#self.connections[room3].remove(room4)
		#self.connections[room4].remove(room3)
		$self->removeBothConnection($room3, $room4);
		$self->propagateNewLabel($room3, $self->findUnusedLabel(), 1);
		$self->propagateNewLabel($room4, $self->findUnusedLabel(), 1);
	}

	# Test 2: Room 3 must not be connected to room 1.

	# 3
	#  \
	#   1
	#  /
	# 2

	$room3 = RoomInfo->new(2 * $room1->{levelNumber} - $room2->{levelNumber}, $room2->{roomNumber});

	#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room1):
	#if (exists $self->{connections}{$room3->toString()} && $self->roomListContainsRoom($self->{connections}{$room3->toString()}, $room1)) {
	#if ($self->roomListContainsRoom($self->{connections}{$room1->toString()}, $room3)) {
	if ($self->roomListContainsRoom($self->{connections}{$room1}, $room3)) {
		#print "Found a Type 2 conflict.\n";
		#self.connections[room1].remove(room3)
		#self.connections[room3].remove(room1)
		$self->removeBothConnection($room1, $room3);
		$self->propagateNewLabel($room3, $self->findUnusedLabel(), 1);
	}

	# Test 3: Room 3 must not be connected to room 2.

	# 3
	#  \
	#   2
	#  /
	# 1

	$room3 = RoomInfo->new(2 * $room2->{levelNumber} - $room1->{levelNumber}, $room1->{roomNumber});

	#if self.connections.has_key(room3) and self.RoomListContainsRoom(self.connections[room3], room2):
	#if (exists $self->{connections}{$room3->toString()} && $self->roomListContainsRoom($self->{connections}{$room3->toString()}, $room2)) {
	#if ($self->roomListContainsRoom($self->{connections}{$room2->toString()}, $room3)) {
	if ($self->roomListContainsRoom($self->{connections}{$room2}, $room3)) {
		#print "Found a Type 3 conflict.\n";
		#self.connections[room2].remove(room3)
		#self.connections[room3].remove(room2)
		$self->removeBothConnection($room2, $room3);
		$self->propagateNewLabel($room3, $self->findUnusedLabel(), 1);
	}

	# Connect room1 and room2.
	#$self->propagateNewLabel($room2, $self->{roomLabels}{$room1->toString()}, 0);
	#push @{$self->{connections}{$room1->toString()}}, $room2;
	#push @{$self->{connections}{$room2->toString()}}, $room1;
	$self->propagateNewLabel($room2, $self->{roomLabels}{$room1}, 0);
	push @{$self->{connections}{$room1}}, $room2;
	push @{$self->{connections}{$room2}}, $room1;

	$self->{numberOfDifferentLabels} = $self->numberOfUniqueRoomLabels(); #len(set(self.roomLabels.values()))
}

sub printRoomLabels {
	my $self = shift;

	for my $room (@{$self->{rooms}}) {
		#print $room->toString() . " is labelled " . $self->{roomLabels}{$room->toString()} . ".\n";
		print $room . " is labelled " . $self->{roomLabels}{$room} . ".\n";
	}
}

sub finalValidityCheck { #(self):
	my $self = shift;

	#print "finalValidityCheck: propagate\n";
	$self->propagateNewLabel(RoomInfo->new(0, 0), $self->findUnusedLabel(), 0);
	#print "finalValidityCheck: done propagate\n";

	#if len(set(self.roomLabels.values())) > 1:
	if ($self->numberOfUniqueRoomLabels() > 1) {
		#$self->printRoomLabels();
		die "The labyrinth is in multiple blobs.";
	}

	#print "The labyrinth is a single blob.\n";
}

#sub AddExtraConnections { #(self):

#sub labelsTest { # This function is just for debugging.
#	my $self = shift;

#	$self->propagateNewLabel(RoomInfo->new(0, 0), $self->findUnusedLabel(), 0);

#	my $cachedNumberOfLabels = $self->{numberOfDifferentLabels};
#	my $actualNumberOfLabels = $self->numberOfUniqueRoomLabels();

#	$cachedNumberOfLabels == $actualNumberOfLabels or die "Number of unique labels is cached as $cachedNumberOfLabels; should be $actualNumberOfLabels.";
#}

sub generate { #(self):
	my $self = shift;
	#my $label = 0;

	$self->{numberOfDifferentLabels} = $self->{numberOfLevels} * $self->{numberOfRoomsPerLevel};

	for my $l (0 .. $self->{numberOfLevels} - 1) {

		for my $r (0 .. $self->{numberOfRoomsPerLevel} - 1) {
			my $room = RoomInfo->new($l, $r);

			push @{$self->{rooms}}, $room;
			#$self->{connections}{$room->toString()} = []; #new List<RoomInfo>();
			$self->{connections}{$room} = []; #new List<RoomInfo>();
		}
	}

	my @roomStrings = keys %{$self->{connections}};

	@{$self->{roomLabels}}{@roomStrings} = (0 .. $#roomStrings);
	@{$self->{openList}} = @{$self->{rooms}};

	#print "generate: Before while\n";
	#$self->labelsTest();

	while ($self->{numberOfDifferentLabels} > 1) {
		#print "generate: Inside while\n";
		#print "Number of unique labels is cached as " . $self->{numberOfDifferentLabels} . "; should be " . $self->numberOfUniqueRoomLabels() . "\n";
		#$self->labelsTest();

        if ((scalar @{$self->{openList}}) == 0) {

            if ($self->{numberOfAttemptsToRefactor} >= $self->{maximumNumberOfAttemptsToRefactor}) {
                die "Attempted to refactor " . $self->{numberOfAttemptsToRefactor} . " times; all failed.";
			}

            ++$self->{numberOfAttemptsToRefactor};
            $self->refactor();
		}

		my $room1Index = int(rand @{$self->{openList}});
        my $room1 = $self->{openList}[$room1Index]; # This must not be $$self...
        my @possibleNeighbours = $room1->generatePossibleNeighbours($self->{numberOfLevels}, $self->{numberOfRoomsPerLevel});
        my $room2 = undef();

        #while (!$room2 && (scalar @possibleNeighbours) > 0) {
        #while (!$room2 && @possibleNeighbours) {
        while (@possibleNeighbours) {
			#print "Choosing room2...\n";
			my $room2Index = int(rand @possibleNeighbours);
            $room2 = $possibleNeighbours[$room2Index]; # This is correct, since @possibleNeighbours is an array, not a reference.
			#print "room1: " . $room1->toString() . "; room2: " . $room2->toString() . "\n";
			#print "room label 1: " . $self->{roomLabels}{$room1->toString()} . "; room label 2: " . $self->{roomLabels}{$room2->toString()} . "\n";

            #if ($self->{roomLabels}{$room1->toString()} != $self->{roomLabels}{$room2->toString()} && !$self->findConflictingConnections($room1, $room2)) {
            if ($self->{roomLabels}{$room1} != $self->{roomLabels}{$room2} && !$self->findConflictingConnections($room1, $room2)) {
				#print "generate(): We want to connect room " . $room1->toString() . " to room " . $room2->toString() . ".\n";
                last; #break
			}

            splice @possibleNeighbours, $room2Index, 1; #.remove(room2)
            $room2 = undef();
		}

        if (!$room2) { # == None:
			#print "No room2 found; removing " . $room1->toString() . " from the open list.\n";
            splice @{$self->{openList}}, $room1Index, 1; #.remove(room1)
            next; #continue
		}

        # We have now chosen room1 and room2.
		#print "generate(): Connecting room " . $room1->toString() . " to room " . $room2->toString() . ".\n";
        #push @{$self->{connections}{$room1->toString()}}, $room2;
        #push @{$self->{connections}{$room2->toString()}}, $room1;
        push @{$self->{connections}{$room1}}, $room2;
        push @{$self->{connections}{$room2}}, $room1;

        # Join the two "blobs" to which the two rooms belong, by modifying room labels.
        #my $label1 = $self->{roomLabels}{$room1->toString()};
        #my $label2 = $self->{roomLabels}{$room2->toString()};
        my $label1 = $self->{roomLabels}{$room1};
        my $label2 = $self->{roomLabels}{$room2};
        my $minLabel = $label1; #min(label1, label2)
        my $maxLabel = $label2; #max(label1, label2)

		if ($label1 > $label2) {
			$minLabel = $label2;
			$maxLabel = $label1;
		}

        for my $room (@{$self->{rooms}}) {

            #if ($self->{roomLabels}{$room->toString()} == $maxLabel) {
            if ($self->{roomLabels}{$room} == $maxLabel) {
                #$self->{roomLabels}{$room->toString()} = $minLabel;
                $self->{roomLabels}{$room} = $minLabel;
			}
		}

        --$self->{numberOfDifferentLabels};
	}

    #if self.numberOfExtraConnections > 0:
    #	self.AddExtraConnections()

	#print "generate: After while\n";
	#print "Number of unique labels is cached as " . $self->{numberOfDifferentLabels} . "; should be " . $self->numberOfUniqueRoomLabels() . "\n";
	#$self->labelsTest();

    #$self->report(); # Instead of this, do the following line:
    $self->finalValidityCheck();

    $self->printLongestPath();		# This sets roomGoal.
    $self->placeBooksInRooms();		# This uses roomGoal.
}

sub report { #(self):
	my $self = shift;

    for my $room (@{$self->{rooms}}) {

		#for my $otherRoom (@{$self->{connections}{$room->toString()}}) {
		for my $otherRoom (@{$self->{connections}{$room}}) {
			#print $room->toString() . " to " . $otherRoom->toString() . "\n";
			print "$room to $otherRoom\n";
		}
	}

    #if (numberOfExtraConnections > 0)

    #	foreach (var extraConnection in extraConnections)
    #    		Console.WriteLine("Extra connection added: {0} to {1}.", extraConnection.Key, extraConnection.Value);

    #	Console.WriteLine("{0} extra connection(s) requested; {1} added.", numberOfExtraConnections, numberOfExtraConnectionsAdded);

    if ($self->{numberOfAttemptsToRefactor} > 0) {
        print "The labyrinth was refactored " . $self->{numberOfAttemptsToRefactor} . " time(s).\n";
	}

    $self->finalValidityCheck();
}

sub findShortestPathBetweenRooms { #(self, room, roomGoalLocal):
	my $self = shift;
	my $room = shift;
	my $roomGoalLocal = shift;
	my $openListLocal = [$room]; #new Queue<RoomInfo>();
	#my %paths; # = {}; # Warning message: "Reference found where even-sized list expected"
	#my %paths = ($room->toString() => [$room]); #new Dictionary<RoomInfo, List<RoomInfo>>();
	my %paths = ($room => [$room]); #new Dictionary<RoomInfo, List<RoomInfo>>();

	#$paths{$room->toString()} = [$room];

	my $pathKeys = [$room];

    #openListLocal.Enqueue(room);
    #paths[room] = new List<RoomInfo>() { room };

    #if room == roomGoalLocal:
    #if ($room->equals($roomGoalLocal)) {
    if ($room == $roomGoalLocal) {
        #return $paths{$room->toString()};
        return $paths{$room};
	}

    #while ((scalar @$openListLocal) > 0) {
    while (@$openListLocal) {
        $room = shift @$openListLocal;

        #for my $room2 (@{$self->{connections}{$room->toString()}}) {
        for my $room2 (@{$self->{connections}{$room}}) {

            #if not (room2 in paths.keys()):	# paths.Keys is essentially the union of openListLocal and closedList.
			if (!$self->roomListContainsRoom($pathKeys, $room2)) {
                push @$openListLocal, $room2;
                push @$pathKeys, $room2;
				#my @clonePathsHelper = @{$paths{$room->toString()}}; # Copy the room's path list by value
				my @clonePathsHelper = @{$paths{$room}}; # Copy the room's path list by value
                #$paths{$room2->toString()} = $self->cloneRoomList($paths{$room->toString()}); # list(r for r in paths[room])
				#$paths{$room2->toString()} = \@clonePathsHelper;
				$paths{$room2} = \@clonePathsHelper;
                #push @{$paths{$room2->toString()}}, $room2;
                push @{$paths{$room2}}, $room2;

                #if ($room2->equals($roomGoalLocal)) {
                if ($room2 == $roomGoalLocal) {
				    #return $paths{$room2->toString()};
				    return $paths{$room2};
				}
			}
		}
	}

    # Here, room is the last room to be dequeued (and thus the last room to be enqueued).
    #return $paths{$room->toString()};
    return $paths{$room};
}

sub findLongestPathFromRoom { #(self, room):
	my $self = shift;
	my $room = shift;
    return $self->findShortestPathBetweenRooms($room, undef());
}

sub printLongestPath { #(self):
	my $self = shift;
    my $path1 = $self->findLongestPathFromRoom(RoomInfo->new($self->{numberOfLevels} - 1, $self->{numberOfRoomsPerLevel} - 1));
	my $lastRoomInPath1 = pop @$path1;
    my $longestPath = $self->findLongestPathFromRoom($lastRoomInPath1);

    #print "\n";
    #Console.WriteLine("The longest path contains {0} rooms:", longestPath.Count);
    #Console.WriteLine(string.Join(" to ", longestPath));
    #print "The longest path contains " . (scalar @$longestPath) . " rooms.\n";

    $self->{roomGoal} = pop @$longestPath;

    #my $pathFromOriginToGoal = $self->findShortestPathBetweenRooms(RoomInfo->new(0, 0), $self->{roomGoal});

    #print "\n";
    #Console.WriteLine("Aristotle's Second Book of the Poetics is in Room {0}.", roomGoal);
    #Console.WriteLine();
    #Console.WriteLine("The path from Room (0, 0) to Room {0} contains {1} rooms:", roomGoal, pathFromOriginToGoal.Count);
    #Console.WriteLine(string.Join(" to ", pathFromOriginToGoal));
    #print "The path from Room (0, 0) to the goal contains " . (scalar @$pathFromOriginToGoal) . " rooms.\n";
}

sub placeBooksInRooms { #(self):
	my $self = shift;
	my $books = [
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
    ];
	my @openListLocalHelper = @{$self->{rooms}}; # Copy the rooms list by value
	my $openListLocal = \@openListLocalHelper;
    my $numBooksPlaced = 1;

    #$self->{booksInRooms}{$self->{roomGoal}->toString()} = "The Second Book of the Poetics of Aristotle";	# Currently, at most one book per room.
    $self->{booksInRooms}{$self->{roomGoal}} = "The Second Book of the Poetics of Aristotle";	# Currently, at most one book per room.

    #openListLocal.remove(self.roomGoal)

	for my $i (0 .. $#$openListLocal) {	# See page 76 for $#

		#if ($$openListLocal[$i]->equals($self->{roomGoal})) {
		if ($$openListLocal[$i] == $self->{roomGoal}) {
			splice @$openListLocal, $i, 1;
			last;
		}
	}

    #while ($numBooksPlaced * 3 < (scalar @{$self->{rooms}}) and (scalar @$books) > 0) {
    while ($numBooksPlaced * 3 < @{$self->{rooms}} && @$books) {
		my $openListLocalIndex = int(rand @$openListLocal);
		my $bookIndex = int(rand @$books);

        my $openListRoom = splice @$openListLocal, $openListLocalIndex, 1;
        my $book = splice @$books, $bookIndex, 1;
        #$self->{booksInRooms}{$openListRoom->toString()} = $book;
        $self->{booksInRooms}{$openListRoom} = $book;
        ++$numBooksPlaced;
	}
}

sub reportProximityToJorge { #(self, room, JorgesRoom):
	my $self = shift;
	my $room = shift;
	my $JorgesRoom = shift;
	my $path = $self->findShortestPathBetweenRooms($room, $JorgesRoom);
	my $distance = $#$path;

    if ($distance == 0) {
        print "* You and the Venerable Jorge are in the same room! *\n";
		print "'Good evening, Venerable Jorge.'\n";
	}
    elsif ($distance <= 2) {
        print "The Venerable Jorge is very near.\n";
	}
    elsif ($distance <= 4) {
        print "The Venerable Jorge is near.\n";
	}
}

sub printPath {
	my $self = shift;
	my $path = shift;
	my $separator = "";

	for my $roomInPath (@$path) {

		if ($roomInPath) {
			#print $separator . $roomInPath->toString();
			print $separator . $roomInPath;
		}
		else {
			print $separator . "null";
		}

		$separator = " to ";
	}

	print ".\n";
}

sub constructJorgesPath { #(self, JorgesRoom):
	my $self = shift;
	my $JorgesRoom = shift;
	my $JorgesGoal;

	do {
        #$JorgesGoal = $self->{rooms}[int(rand(scalar @{$self->{rooms}}))]; # This must not be $$self...
        $JorgesGoal = $self->{rooms}[int(rand @{$self->{rooms}})]; # This must not be $$self...
	}
	#while ($JorgesGoal->equals($JorgesRoom));
	while ($JorgesGoal == $JorgesRoom);

    #print "constructJorgesPath: Jorge's destination is room " . $JorgesGoal->toString() . ".\n";
    return $self->findShortestPathBetweenRooms($JorgesRoom, $JorgesGoal);
    #my $path = $self->findShortestPathBetweenRooms($JorgesRoom, $JorgesGoal);
	#$self->printPath($path);
	#return $path;
}

sub navigateLabyrinth { #(self):
	my $self = shift;
	my $roomsVisited = []; #new HashSet<RoomInfo>();
	my $room = RoomInfo->new(0, 0);

    #Console.WriteLine("Selecting a room for Jorge out of {0} rooms.", rooms.Count);

	#print "Selecting Jorge's initial room...\n";
    #my $JorgesRoom = $self->{rooms}[int(rand(scalar @{$self->{rooms}}))]; # This must not be $$self...
    my $JorgesRoom = $self->{rooms}[int(rand @{$self->{rooms}})]; # This must not be $$self...
    #print "The Venerable Jorge is now in room " . $JorgesRoom->toString() . ".\n";
    my $JorgesPath = $self->constructJorgesPath($JorgesRoom);
	#print "Jorge's initial path has been constructed.\n";
    my $JorgesPathIndex = 0;

    while (1) {
        #roomsVisited.Add(room);

		#if not room in roomsVisited:
		if (!$self->roomListContainsRoom($roomsVisited, $room)) {
			push @$roomsVisited, $room;
		}

        print "\n";
        #print "You are now in room " . $room->toString() . ".\n";
        print "You are now in room $room.\n";
        #print "The Venerable Jorge is now in room " . $JorgesRoom->toString() . ".\n";
		my $lengthOfJorgesPath = @$JorgesPath;
		#print "Length of Jorge's path: $lengthOfJorgesPath\n";
		my $JorgesGoal = $$JorgesPath[$lengthOfJorgesPath - 1];
        #print "Jorge's destination is room " . $JorgesGoal->toString() . ".\n";

        $self->reportProximityToJorge($room, $JorgesRoom);

        #if (exists $self->{booksInRooms}{$room->toString()}) {
        if (exists $self->{booksInRooms}{$room}) {
            #print "You have found the book '" . $self->{booksInRooms}{$room->toString()} . "'.\n";
            print "You have found the book '" . $self->{booksInRooms}{$room} . "'.\n";
		}

        #if room == self.roomGoal:
        #if ($room->equals($self->{roomGoal})) {
        if ($room == $self->{roomGoal}) {
            print "**** Congratulations!  You have reached the goal! ****\n";
		}

        #my $neighbouringRooms = $self->{connections}{$room->toString()};
        my $neighbouringRooms = $self->{connections}{$room};

        print "Possible moves:\n";

		#for my $neighbouringRoom (@$neighbouringRooms) {
		#	print $neighbouringRoom->toString() . "\n";
		#}

		for my $i (0 .. $#$neighbouringRooms) {
		#for my $i (0 .. ((scalar @$neighbouringRooms) - 1)) {
            my $neighbouringRoom = $$neighbouringRooms[$i];
            #my $s = "  " . $i . ". " . $neighbouringRoom->toString();	# "s" is for "string".
            my $s = "  $i. $neighbouringRoom";	# "s" is for "string".

			#if neighbouringRoom in roomsVisited:
			if ($self->roomListContainsRoom($roomsVisited, $neighbouringRoom)) {
                $s = $s . " Visited";
			}

            print $s . "\n";
		}

		print "Your move (or (h)elp or (q)uit): ";
        my $inputStr = <STDIN>;
		chomp($inputStr);
		my $isInt = ($inputStr =~ /^(0|[1-9][0-9]*)$/); # See http://stackoverflow.com/questions/1112983/in-perl-how-can-i-tell-if-a-string-is-a-number

		if ($isInt) {

            if ($inputStr < 0 || $inputStr > $#$neighbouringRooms) {
                print "The input is out of range.\n";
			}
            else {
                $room = $$neighbouringRooms[$inputStr];
                $self->reportProximityToJorge($room, $JorgesRoom);
			}
		}
        elsif ($inputStr eq "") {
            print "The input is empty.\n";
		}
        elsif ($inputStr eq "h") {
            print "Path to goal: ";
			$self->printPath($self->findShortestPathBetweenRooms($room, $self->{roomGoal}));
		}
        elsif ($inputStr eq "q") {
            last; #break
		}
		else {
			print "The input was not recognized.\n";
		}

        # Jorge's move.
		#print "Jorge's move.\n";
        ++$JorgesPathIndex;

        while ($JorgesPathIndex > $#$JorgesPath) { # ThAW 2013/09/23 : This "while" used to be an "if", but it crashed once.
			#print "Finding a new path for Jorge...\n";
            $JorgesPath = $self->constructJorgesPath($JorgesRoom);
            $JorgesPathIndex = 1;
		}

        $JorgesRoom = $$JorgesPath[$JorgesPathIndex];
	}
}

1; # The module must return a true value.
