# The labyrinthine abbey library in Perl - October 9, 2013

package RoomInfo;

use strict;
use warnings FATAL => qw(all);
use overload
	'==' => \&equals,
	'""' => \&toString;

sub new {
	# See page 338.
	my $invocant = shift;
	my $self = bless({}, ref $invocant || $invocant);
	$self->{levelNumber} = shift;
	$self->{roomNumber} = shift;
	return $self;
}

sub equals {
	my $self = shift;
	my $other = shift;
	return $other &&	# Return false if $other is undef()
		ref $self eq ref $other &&
		$self->{levelNumber} == $other->{levelNumber} &&
		$self->{roomNumber} == $other->{roomNumber};
}

sub toString {
	my $self = shift;
	return "($self->{levelNumber}, $self->{roomNumber})";
}

sub generatePossibleNeighboursOnLevel {
	my $self = shift;
	my $numberOfRoomsPerLevel = shift;
	my $newLevel = shift;
	my @result = ();

	if ($self->{roomNumber} == $numberOfRoomsPerLevel - 1) {

		for my $i (0 .. $numberOfRoomsPerLevel - 2) {
			push @result, RoomInfo->new($newLevel, $i);
		}
	}
	else {
		push @result, RoomInfo->new($newLevel, ($self->{roomNumber} + 1) % ($numberOfRoomsPerLevel - 1));
		push @result, RoomInfo->new($newLevel, ($self->{roomNumber} + $numberOfRoomsPerLevel - 2) % ($numberOfRoomsPerLevel - 1));
		push @result, RoomInfo->new($newLevel, $numberOfRoomsPerLevel - 1);
	}

	return @result;
}

sub generatePossibleNeighbours {
	my $self = shift;
	my $numberOfLevels = shift;
	my $numberOfRoomsPerLevel = shift;
	my @result = ();

	if ($self->{levelNumber} > 0) {

		for my $neigh ($self->generatePossibleNeighboursOnLevel($numberOfRoomsPerLevel, $self->{levelNumber} - 1)) {
			push @result, $neigh;
		}
	}

	if ($self->{levelNumber} < $numberOfLevels - 1) {

		for my $neigh ($self->generatePossibleNeighboursOnLevel($numberOfRoomsPerLevel, $self->{levelNumber} + 1)) {
			push @result, $neigh;
		}
	}

	return @result;
}

1;	# The module must return a true value.
