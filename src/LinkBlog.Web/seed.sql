BEGIN;

-- Insert dummy posts with HTML-formatted content
INSERT INTO "Posts" ("Id", "Title", "Date", "Link", "LinkTitle", "Contents")
VALUES 
    ('post-1', 'Neapolitan Pizza Experience', '2025-02-08 18:00:00+00', 'http://example.com', 'Pizza Pizza', '<p>The thin crust and fresh marinated tomatoes made this pizza an unforgettable experience.</p>'),
    ('post-2', 'Sushi Delights', '2025-02-09 19:00:00+00', NULL, NULL, '<p>Savory flavors and perfectly seasoned rice turned this sushi dinner into a true art form.</p>'),
    ('post-3', 'Vegan Salad Bowl', '2025-02-10 12:00:00+00', NULL, NULL, '<p>A colorful mix of crisp greens, roasted chickpeas, and a tangy lemon dressing.</p>'),
    ('post-4', 'Mexican Tacos Adventure', '2025-02-11 13:30:00+00', NULL, NULL, '<p>Spicy beef, fresh salsa, and hand-made tortillas made this taco tasting a vibrant experience.</p>'),
    ('post-5', 'French Pastries Morning', '2025-02-12 09:00:00+00', NULL, NULL, '<p>Flaky croissants and buttery pain au chocolat that captivated my sweet tooth.</p>'),
    ('post-6', 'Thai Green Curry Evening', '2025-02-12 20:00:00+00', NULL, NULL, '<p>A harmonious blend of spicy chilies and creamy coconut milk in this vibrant curry.</p>'),
    ('post-7', 'Indian Butter Chicken Feast', '2025-02-13 18:45:00+00', NULL, NULL, '<p>Creamy tomato sauce and tender chicken pieces cooked to perfection with aromatic spices.</p>'),
    ('post-8', 'Mediterranean Pasta Lunch', '2025-02-13 13:00:00+00', NULL, NULL, '<p>Pasta tossed with olives, feta, and sun-dried tomatoes for a burst of flavor.</p>'),
    ('post-9', 'Berry Smoothie Break', '2025-02-14 08:30:00+00', NULL, NULL, '<p>A blend of fresh strawberries, blueberries, and a dollop of yogurt for a perfect morning boost.</p>'),
    ('post-10', 'Gourmet Burger Night', '2025-02-14 21:00:00+00', NULL, NULL, '<p>An artisanal burger featuring a rich blend of spices, melted cheese, and caramelized onions.</p>');

-- Insert tags
INSERT INTO "Tags" ("Id", "Name")
VALUES 
    ('tag-1', 'Italian'),
    ('tag-2', 'Japanese'),
    ('tag-3', 'Healthy'),
    ('tag-4', 'Street Food'),
    ('tag-5', 'Dessert'),
    ('tag-6', 'Thai'),
    ('tag-7', 'Indian');

-- Insert relationships between posts and tags
INSERT INTO "PostTag" ("PostsId", "TagsId")
VALUES 
    ('post-1', 'tag-1'),
    ('post-2', 'tag-2'),
    ('post-3', 'tag-3'),
    ('post-4', 'tag-4'),
    ('post-4', 'tag-7'),
    ('post-5', 'tag-5'),
    ('post-6', 'tag-6'),
    ('post-7', 'tag-7'),
    ('post-8', 'tag-1'),
    ('post-8', 'tag-3'),
    ('post-9', 'tag-3'),
    ('post-9', 'tag-5'),
    ('post-10', 'tag-4');

COMMIT;