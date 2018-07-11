#!/usr/bin/perl
# The -w option enables additional warnings, which are useful for debugging; see page 20 of the book.

# The labyrinthine abbey library in Perl - October 9, 2013

use strict;
use warnings FATAL => qw(all); # Make all warnings fatal.  See page 862.

#use RoomInfo;
use LabyrinthGenerator;

#my $room = RoomInfo->new(14, 6);
#print $room->toString() . "\n";

#my $room2 = RoomInfo->new(14, 6);
#my $room3 = RoomInfo->new(0, 0);
#print "(14, 6) equals (14, 6): '" . $room->equals($room2) . "'\n";
#print "(14, 6) equals (0, 0): '" . $room->equals($room3) . "'\n";

#my $room4 = RoomInfo->new(10, 6);
#my @neighs = $room4->generatePossibleNeighbours(15, 7);

#for my $neigh (@neighs) {
#	print "Neighbour of " . $room4->toString() . ": " . $neigh->toString() . "\n";
#}

my $numberOfLevels = 15;
my $numberOfRoomsPerLevel = 7;

if (@ARGV >= 2)
{
	$numberOfLevels = $ARGV[0];
	$numberOfRoomsPerLevel = $ARGV[1];
}

my $generator = LabyrinthGenerator->new($numberOfLevels, $numberOfRoomsPerLevel);

#my $roomList = [RoomInfo->new(0, 0), RoomInfo->new(0, 1), RoomInfo->new(0, 2)];
#print "roomList: $roomList\n";
#print "True: " . $generator->roomListContainsRoom($roomList, RoomInfo->new(0, 1)) . "\n";
#print "False: " . $generator->roomListContainsRoom($roomList, RoomInfo->new(0, 3)) . "\n";

#$generator->{roomLabels}{$room2->toString()} = 0;
#$generator->{roomLabels}{$room3->toString()} = 1;
#print "Label 1 is used: " . $generator->labelIsUsed(1) . "\n";
#print "Label 3 is used: " . $generator->labelIsUsed(3) . "\n";
#print "First unused label: " . $generator->findUnusedLabel() . "\n";

#$oneToFive = [1, 2, 3, 4, 5];

#while ((scalar @$oneToFive) > 0) {
#	my $n = splice @$oneToFive, int(rand(scalar @$oneToFive)), 1;
#	print $n . "\n";
#}

# $hashSource = ["A", 0, "B", 1, "C", 2, "A", 3];
#$hashSourceKeys = ["A", "B", "C", "A"];
# @hashSourceValues = (0) x 4; # (0, 0, 0, 0)
# %hash1 = @hashSource;
# @hash1{@$hashSourceKeys} = @hashSourceValues;
#@hash1{@$hashSourceKeys} = (0) x (scalar @hashSourceKeys);
# $hash1{"A"} = 0;
# $hash1{"B"} = 0;
# $hash1{"C"} = 0;
#$sizeofHash1 = scalar keys %hash1;
#print "Size of hash (3): " . $sizeofHash1 . "\n";

#for my $hashkey (keys %hash1) {
#	print "  Key: " . $hashkey . "\n";
#}

#$generator->{roomLabels} = {};
#$generator->{roomLabels}{RoomInfo->new(0, 0)->toString()} = 0;
#$generator->{roomLabels}{RoomInfo->new(1, 0)->toString()} = 1;
#$generator->{roomLabels}{RoomInfo->new(2, 0)->toString()} = 7;
#$generator->{roomLabels}{RoomInfo->new(3, 0)->toString()} = 1;
#$generator->{roomLabels}{RoomInfo->new(4, 0)->toString()} = 0;
#$numKeys = scalar keys %{$generator->{roomLabels}};
#print "Number of keys (5): " . $numKeys . "\n";
#$numLabels = $generator->numberOfUniqueRoomLabels();
#print "Number of unique labels (3): " . $numLabels . "\n";

#print "Enter something: ";
#my $chompValue = chomp($lineOfText = <STDIN>);
#my $isInt = ($lineOfText =~ /^(0|[1-9][0-9]*)$/); # See http://stackoverflow.com/questions/1112983/in-perl-how-can-i-tell-if-a-string-is-a-number
#print "Line of text: " . $lineOfText . "; int: " . $isInt . "; chompValue: " . $chompValue . "\n";

#my $arrayToIndex = [2, 3, 5, 7];
#print "The fourth prime number is " . $$arrayToIndex[3] . "\n";
#print "Last array index is " . $#$arrayToIndex . "\n";

#$generator = LabyrinthGenerator->new(15, 7);
$generator->generate();

#my $middleRoom = $generator->{rooms}[52]; # $generator..., not $$generator...
#print "Middle room: " . $middleRoom->toString() . "\n";

$generator->navigateLabyrinth();
