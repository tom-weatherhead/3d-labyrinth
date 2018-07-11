#!/usr/bin/perl

# The labyrinthine abbey library as a Perl Web app - June 23, 2014

use strict;
use warnings FATAL => qw(all); # Make all warnings fatal.  See page 862.
no warnings qw(redefine);

use CGI qw/:standard/; # Load standard CGI routines.
#use CGI qw/:all/;
use DBI;
use HTML::Table;
use Template;

use lib "/var/www/html/perl/labyrinth";
use RoomInfo;

#my $cgi = CGI->new;
#my $foo = param('foo') || 'undefined'; # param() can return a scalar or a list.
my $dsn = "DBI:mysql:database=labyrinth;host=localhost";
our $dbh = DBI->connect($dsn, "user", "tomtom7", { RaiseError => 1 }) or die "Connection Error: $DBI::errstr\n";

sub hasBeenVisited { #($db, $level, $room) {
	my $level = shift;
	my $room = shift;
	my $sql = "SELECT * FROM visited WHERE level = ? AND room = ?";
	my $sth = $dbh->prepare($sql);
	$sth->execute($level, $room) or die "SQL Error when selecting from visited: $DBI::errstr\n";
	return defined($sth->fetchrow_array());
}

sub getBooksInRoom { #($db, $level, $room) {
	# Find the book(s) in this room, if any.
	my $level = shift;
	my $room = shift;
	my $sql = "SELECT name FROM books WHERE level = ? AND room = ?";
	my $sth = $dbh->prepare($sql);
	$sth->execute($level, $room) or die "SQL Error when selecting from books: $DBI::errstr\n";
	my @bookList;

	while (my @row = $sth->fetchrow_array()) {
		push @bookList, $row[0];
	}

	return @bookList;
}

sub isGoal { #($db, $level, $room) {
	my $level = shift;
	my $room = shift;
	my @bookList = getBooksInRoom($level, $room);

	for my $book (@bookList) {
		# Look for "The Second Book of the Poetics of Aristotle".
		return 1 if $book =~ /Second/;
	}

	return 0;
}

sub getConnections { #($db, $level, $room) {
	# Find the connections from this room to other rooms.
	my $level = shift;
	my $room = shift;
	my $sql = "SELECT level2, room2 FROM connections WHERE level1 = ? AND room1 = ?"; # order by level2, room2 ?
	my $sth = $dbh->prepare($sql);
	$sth->execute($level, $room) or die "SQL Error when selecting from connections: $DBI::errstr\n";
	my @roomList;

	while (my @row = $sth->fetchrow_array()) {
		my $roomInfo = RoomInfo->new($row[0], $row[1]);

		$roomInfo->{pathToGoal} = "No path generated.";
		push @roomList, $roomInfo;
	}

	return @roomList;
}

sub arrayContainsRoom { #($array, $roomInfo) {
	my $array = shift;
	my $roomInfo = shift;

	for (@$array) {
		return 1 if $_ == $roomInfo;
	}

	return 0;
}

sub getPathToGoal { #($db, $level, $room) {
	my $level = shift;
	my $room = shift;
	my $roomInfo = RoomInfo->new($level, $room);
	$roomInfo->{pathToGoal} = $roomInfo->toString();
	my $queue = [$roomInfo];
	my $closedSet = [];

	while (@$queue) {
		$roomInfo = shift @$queue;
		push @$closedSet, $roomInfo;

		if (isGoal($roomInfo->{levelNumber}, $roomInfo->{roomNumber})) {
			return $roomInfo->{pathToGoal};
		}

		for my $connectedRoom (getConnections($roomInfo->{levelNumber}, $roomInfo->{roomNumber})) {

			if (!arrayContainsRoom($queue, $connectedRoom) && !arrayContainsRoom($closedSet, $connectedRoom)) {
				$connectedRoom->{pathToGoal} = $roomInfo->{pathToGoal} . " to $connectedRoom";
				push @$queue, $connectedRoom;
			}
		}
	}

	return "No path to the goal was found.";
}

my $level = param("level");
my $room = param("room");

if (!defined($level) || !defined($room)) {
	# Clear the "visited" table.
	$dbh->prepare("DELETE FROM visited")->execute or die "SQL Error when deleting all visited: $DBI::errstr\n";
	#$dbh->prepare("DELETE FROM visited WHERE level > 0 OR room > 0")->execute or die "SQL Error when deleting all visited: $DBI::errstr\n";
	#$dbh->commit() or die "Database Error when committing deletion of all visited: $DBI::errstr\n"; # This is ineffective when AutoCommit is enabled.
	$level = 0;
	$room = 0;
}

if (!hasBeenVisited($level, $room)) {
	# Insert this room into the "visited" table.
	$dbh->prepare("INSERT INTO visited (level, room) VALUES (?, ?)")->execute($level, $room)
		or die "SQL Error when inserting into visited: $DBI::errstr\n";
}

my $booksInRoom = "";

for my $book (getBooksInRoom($level, $room)) {
	$booksInRoom .= "<p>The book \"$book\" is in this room.</p>";
}

my $goalNotification = "";

if (isGoal($level, $room)) {
	$goalNotification = "<p>**** Congratulations! You have reached the goal! ****</p>";
}

my $connectionsHTMLTable = new HTML::Table(-cols => 2, -head => ["Room", "Visited"]);

for my $connectedRoom (getConnections($level, $room)) {
	my $level2 = $connectedRoom->{levelNumber};
	my $room2 = $connectedRoom->{roomNumber};

	$connectionsHTMLTable->addRow(
		"<a href=\"index.cgi?level=$level2&room=$room2\">$connectedRoom</a>",
		hasBeenVisited($level2, $room2) ? "True" : "False");
}

my $helpLinkOrPath;

if (param("help")) {
	$helpLinkOrPath = "<p>Path to goal: " . getPathToGoal($level, $room) . "</p>";
} else {
	$helpLinkOrPath = "<p><a href=\"index.cgi?level=$level&room=$room&help=1\">Help</a></p>";
}

# Render the Web page from the template.
my $vars = {
	"room" => RoomInfo->new($level, $room),
	"booksInRoom" => $booksInRoom,
	"goalNotification" => $goalNotification,
	"connectionsHTMLTable" => $connectionsHTMLTable,
	"helpLinkOrPath" => $helpLinkOrPath
};

my $template = Template->new({
	INCLUDE_PATH => "/var/www/html/perl/labyrinth" #,
	# PRE_PROCESS  => "config"
});

print header(-type => "text/html",
             -charset => "utf-8");

$template->process("index.tt", $vars)
	|| die $template->error();
