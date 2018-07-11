#!/usr/bin/perl

# The labyrinthine abbey library as a Perl Web app - June 23, 2014

use strict;
use warnings FATAL => qw(all); # Make all warnings fatal.  See page 862.

use CGI qw/:standard/; # Load standard CGI routines.
#use CGI qw/:all/;
use DBI;
use HTML::Table;
use Template;

use lib "/var/www/html/perl/labyrinth";
use LabyrinthGenerator;

my $labyrinthGenerator = LabyrinthGenerator->new(15, 7);

$labyrinthGenerator->generate();

#my $cgi = CGI->new;
#my $foo = param('foo') || 'undefined'; # param() can return a scalar or a list.
my $connectionsHTMLTable = new HTML::Table(-cols => 2, -head => ["From", "To"]);
my $booksHTMLTable = new HTML::Table(-cols => 2, -head => ["Room", "Book"]);
my $dsn = "DBI:mysql:database=labyrinth;host=localhost";
my $dbh = DBI->connect($dsn, "user", "tomtom7", { RaiseError => 1 }) or die "Connection Error: $DBI::errstr\n";

# 1) Delete all old connections.
my $sql = "DELETE FROM connections";
my $sth = $dbh->prepare($sql);
$sth->execute or die "SQL Error when deleting all connections: $DBI::errstr\n";

# 2) Delete al old books.
$sql = "DELETE FROM books";
$sth = $dbh->prepare($sql);
$sth->execute or die "SQL Error when deleting all books: $DBI::errstr\n";

# 3) Insert all new connections.
$sql = "INSERT INTO connections (level1, room1, level2, room2) VALUES (?, ?, ?, ?)";
$sth = $dbh->prepare($sql);

#while (my ($room1, $room1Connections) = each %{$labyrinthGenerator->{connections}}) {
#for my $room1 (keys %{$labyrinthGenerator->{connections}}) {
for my $room1 (@{$labyrinthGenerator->{rooms}}) {

	#for my $room2 (@$room1Connections) {
	for my $room2 (@{$labyrinthGenerator->{connections}{$room1}}) {
		$sth->execute($room1->{levelNumber}, $room1->{roomNumber},
			$room2->{levelNumber}, $room2->{roomNumber})
			or die "SQL Error when inserting connection: $DBI::errstr\n";
		$connectionsHTMLTable->addRow($room1->toString(), $room2->toString());
	}
}

# 4) Insert all new books.
$sql = "INSERT INTO books (level, room, name) VALUES (?, ?, ?)";
$sth = $dbh->prepare($sql);

#while (my ($room, $book) = each %{$labyrinthGenerator->{booksInRooms}}) {
#	$sth->execute($room->{levelNumber}, $room->{roomNumber}, $book) or die "SQL Error when inserting book: $DBI::errstr\n";
#	$booksHTMLTable->addRow($room->toString(), $book);
#}

for my $room (@{$labyrinthGenerator->{rooms}}) {

	if (exists $labyrinthGenerator->{booksInRooms}{$room}) {
		my $book = $labyrinthGenerator->{booksInRooms}{$room};

		$sth->execute($room->{levelNumber}, $room->{roomNumber}, $book) or die "SQL Error when inserting book: $DBI::errstr\n";
		$booksHTMLTable->addRow($room->toString(), $book);
	}
}

# Render the Web page from the template.
my $vars = {
	"connectionsHTMLTable" => $connectionsHTMLTable,
	"booksHTMLTable" => $booksHTMLTable
};

my $template = Template->new({
	INCLUDE_PATH => "/var/www/html/perl/labyrinth" #,
	# PRE_PROCESS  => "config"
});

print header(-type => "text/html",
             -charset => "utf-8");

$template->process("gen.tt", $vars)
	|| die $template->error();
