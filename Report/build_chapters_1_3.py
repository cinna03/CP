#!/usr/bin/env python3
"""Chapters 1–3 of the capstone report, checked against the proposal and the build."""

from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_LINE_SPACING, WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn

OUT = "/Users/cikirezi/Library/CloudStorage/OneDrive-ThePartnershipAfrica/Documents/GitHub/CP/Report/Mwiza_Cannelle_Capstone_Report_Chapters_1-3.docx"


def set_run(run, size=12, bold=False, italic=False):
    run.font.name = "Times New Roman"
    run._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor(0, 0, 0)


def p(doc, text, *, size=12, bold=False, italic=False, center=False, space_after=8, space_before=0, first=True):
    para = doc.add_paragraph()
    para.paragraph_format.space_after = Pt(space_after)
    para.paragraph_format.space_before = Pt(space_before)
    para.paragraph_format.line_spacing_rule = WD_LINE_SPACING.ONE_POINT_FIVE
    if center:
        para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    else:
        para.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        if first:
            para.paragraph_format.first_line_indent = Cm(1.25)
    run = para.add_run(text)
    set_run(run, size, bold, italic)
    return para


def h(doc, text, level):
    para = doc.add_heading(text, level=level)
    for run in para.runs:
        set_run(run, 14 if level == 1 else 13 if level == 2 else 12, bold=True)
    para.paragraph_format.space_before = Pt(16 if level == 1 else 12)
    para.paragraph_format.space_after = Pt(8)
    return para


def add_table(doc, headers, rows):
    table = doc.add_table(rows=1 + len(rows), cols=len(headers))
    table.style = "Table Grid"
    for i, header in enumerate(headers):
        cell = table.rows[0].cells[i]
        cell.text = ""
        run = cell.paragraphs[0].add_run(header)
        set_run(run, 11, bold=True)
    for r, row in enumerate(rows):
        for c, value in enumerate(row):
            cell = table.rows[r + 1].cells[c]
            cell.text = ""
            run = cell.paragraphs[0].add_run(value)
            set_run(run, 11)
    doc.add_paragraph()


def build():
    doc = Document()
    section = doc.sections[0]
    section.top_margin = Cm(2.54)
    section.bottom_margin = Cm(2.54)
    section.left_margin = Cm(2.54)
    section.right_margin = Cm(2.54)
    normal = doc.styles["Normal"]
    normal.font.name = "Times New Roman"
    normal.font.size = Pt(12)

    p(doc, "Chapters 1 to 3", size=16, bold=True, center=True, first=False, space_after=4)
    p(doc, "An Augmented Reality Artwork Visualization and Decision-Support Prototype for Reducing Buyer Uncertainty in Online Purchase of Physical Artworks in Kigali", size=12, italic=True, center=True, first=False, space_after=2)
    p(doc, "Mwiza Louange Cannelle", center=True, first=False, space_after=2)
    p(doc, "Supervisor: Tunde Isiaq Gbadamosi", center=True, first=False, space_after=12)
    p(doc, "These three chapters are written in the past tense from the approved proposal and from the prototype that was built. The abstract, the preliminary pages, Chapter 4, and Chapter 5 are still to be written. Figure boxes are left where a diagram belongs, so that a copied database diagram from the proposal is not presented as the build.", first=False, italic=True, space_after=16)

    # CHAPTER 1
    h(doc, "CHAPTER ONE: INTRODUCTION", 1)

    h(doc, "1.1 Background to the Study", 2)
    p(doc, "Buying online is no longer a side channel. Across 43 economies that account for about three-quarters of global GDP, businesses generated nearly $25 trillion in e-commerce sales in 2021 (United Nations Conference on Trade and Development [UNCTAD], 2024a). Art has followed the same path, and then settled into something more mixed. Arts Economics (2025) reported that online sales of art and antiques fell 11 percent to $10.5 billion in 2024. That was still 18 percent of global art-market sales, and 76 percent above the 2019 level. The following year the online total fell again, to $9.2 billion, and the online share dropped to 15 percent as many of the highest-value sales moved back into rooms where the work could be seen in place (Arts Economics, 2026). People still buy physical works through a screen. The expensive ones, more often than not, are still decided where the buyer can stand in front of them.")
    p(doc, "A painting is a hard thing to judge from a product page. The buyer has to form a view of how it looks, how large it really is, whether it belongs on a particular wall, and whether it is the piece they thought they wanted. A photograph flattens lighting, texture, and scale. In a gallery those cues are available for free, just by walking up to the wall. Online, they have to be reconstructed.")
    p(doc, "Kigali is the setting of this study, not the global market as a whole. A 2020 diagnostic cited by the Government of Rwanda found the national e-commerce ecosystem still at an early stage, and the same contribution names lack of consumer trust as a barrier that the Digital Business Institute had tried to address with a trust seal for platforms (UNCTAD, 2024b). That material does not say how a person in Kigali judges a physical artwork online. It does say that online purchase decisions in Rwanda are made where trust is still being built, and where a buyer who cannot inspect a work in the intended room has little local evidence to lean on.")
    p(doc, "The people most directly affected are adults in Kigali who want to buy a physical artwork online and cannot stand in front of it first. Artists and galleries are affected as well. A buyer who is unsure about scale and fit may wait, or may not buy. This study treated buyers as the people the prototype was for. Checking that a story note is fair to the work is a separate, smaller task, and it was not the evaluation itself.")

    h(doc, "1.2 Statement of the Problem", 2)
    p(doc, "Online commerce has made it ordinary to buy something without touching it, and physical artworks are now offered that way through galleries and marketplaces. An artwork is not a standard packaged good. Its value for a buyer sits in how it looks, how it sits in a room, and what the buyer makes of its story. Those are judgements that usually happen in front of the work. When the work is only a picture on a page, the buyer is left to guess at appearance, size, fit, and whether the piece matches what they want. Chen et al. (2021) showed that ordinary ratings are better at carrying objective product information than at settling a question of personal fit. Pfaff and Spann (2025) showed that the difficulty is sharper still when the product has to occupy a real space, because suitability depends on the room it will enter.")
    p(doc, "Product pages do try to help. They offer descriptions, photographs, reviews, and recommendations. Those tools still leave a particular kind of doubt in place. A still image does not show how a work will read on the buyer’s own wall. A recommendation can match a taste and still say nothing about whether the piece will fit the room. Reviews tend to speak to what a product is, more than to whether it is right for one person in one space (Chen et al., 2021). A buyer can therefore know a great deal about an artwork and still be unsure about appearance, scale, spatial fit, and preference.")
    p(doc, "There is evidence that augmented reality can help a shopper see a product in place, and that a conversation can help a shopper say what they are unsure about (Liu et al., 2023; Pfaff & Spann, 2025). What is much harder to find is one system, built for physical artworks, that puts a true-scale preview, a written reply to the buyer’s question, a short story note, and a recommendation that changes after the buyer keeps, saves, or dismisses a work, into the same visit. That was the problem this study took up. The aim was a prototype that would give a buyer in Kigali those four kinds of information before a purchase, and that would do so in a way the researcher could read and check.")

    h(doc, "1.3 Aim and Objectives of the Study", 2)
    p(doc, "The aim of this study was to design, implement, and evaluate an augmented reality artwork visualization and decision-support prototype that reduces buyer uncertainty among prospective online artwork buyers in Kigali.")
    p(doc, "The specific objectives were these.", first=False)
    p(doc, "1. To identify the factors that contribute to buyer uncertainty when a physical artwork is considered online, and to derive the system requirements that address those factors.", first=False)
    p(doc, "2. To design and implement a prototype that places an artwork at its stored size, answers from a written rule table, ranks other works from the buyer’s decision, and keeps a story note with the work.", first=False)
    p(doc, "3. To evaluate, with buyers, the change in uncertainty from a static product page to the prototype, together with decision confidence, recommendation relevance, usability, satisfaction, and contextual understanding.", first=False)
    p(doc, "The requirements used for the build were the ones set out in the approved proposal: true-scale placement, a reply the researcher could audit, a recommendation based on style, mood, and colour, a stored story note, and a new ranking after the buyer’s decision. Chapters 2 and 3 explain that design and the prototype that was built for it. Chapter 4 reports how the prototype was tested, and the status of the participant evaluation.")

    h(doc, "1.4 Research Questions", 2)
    p(doc, "The study was guided by three questions.", first=False)
    p(doc, "1. What factors contribute to buyer uncertainty among adults in Kigali who are considering an online purchase of a physical artwork, and which system requirements follow from those factors?", first=False)
    p(doc, "2. How can a prototype implement true-scale augmented-reality visualization, rule-based conversational guidance, personalized recommendation, cultural storytelling, and feedback-driven re-ranking as one buyer workflow?", first=False)
    p(doc, "3. What change in buyer uncertainty and decision confidence is observed after buyers use the prototype, and how do they rate recommendation relevance, usability, satisfaction, and contextual understanding?", first=False)
    p(doc, "The second question is the one this report can answer from the system itself. The first and the third depend on what buyers say. They are kept here because they shaped the design, and they are taken up again when the results are discussed.")

    h(doc, "1.5 Project Scope", 2)
    p(doc, "The study was limited to the design, development, and planned evaluation of a prototype that supports an online decision about a physical artwork. The intended buyers were adults in Kigali with a basic comfort with a phone and an interest in buying online or in buying art. A sample of that kind cannot stand in for every prospective buyer in the city, and the study did not claim that it could.")
    p(doc, "The work was about physical artworks, not about online shopping in general. The prototype was not a shop. It did not take payment, verify a seller, manage stock, track a delivery, or detect fraud. Those tasks sit outside a decision about appearance, scale, fit, and preference.")
    p(doc, "What the prototype did cover was the path a buyer needs before that decision. An artwork was stored with its height and width in centimetres, and the wall preview used those centimetres divided by 100, so the size on the wall was the stored size. The buyer could ask about size, colour, or the story note and receive a reply from a written table in English, French, or Kinyarwanda. A question the table did not cover received one fixed line, and no new fact was added. After the buyer kept, saved, or dismissed a work, the visit ranked three other works and left the current one out. The story note travelled with the record and was shown when the buyer asked for it.")
    p(doc, "The prototype was built in Unity and run on iOS, using AR Foundation so that a tap could place the work on a vertical wall. The artwork records were kept inside the application. A visit therefore did not depend on a separate catalogue server. That was a deliberate boundary for this build: the decision-support behaviour could be tested on the device the researcher had, and the same size rule served both the editor visit and the phone session.")

    h(doc, "1.6 Significance and Justification", 2)
    p(doc, "If a buyer can see a work at its real size on a wall, the guesswork about scale and fit becomes a smaller part of the decision. That is the practical point of the prototype. A reply that comes from a written table, a short note about the work, and a ranking that moves when the buyer keeps or dismisses a piece, give the same visit somewhere to go after the preview. The buyer is not left with a picture and a price.")
    p(doc, "The study also had a narrower academic point. The literature already supports each of these pieces on its own: a preview in the room, a conversation that draws out what the shopper wants, a recommendation, and a note that carries meaning. It is much quieter on what happens when those pieces share one visit, and quieter still on that question in Kigali. A working prototype is the honest way to ask it. The prototype can be opened, the replies can be read, and the ranking can be traced back to a decision. Whether buyers themselves feel less unsure is a separate question, and it is reserved for the results.")

    h(doc, "1.7 Research Budget", 2)
    p(doc, "The build was scoped so that it could be completed with tools the researcher already had. Unity was used under the personal licence. The phone was an iPhone 13 already owned by the researcher, and the development machine was the researcher’s own laptop. Version control was GitHub. No new device was bought for the prototype, and no paid model was called to write replies, because every reply was taken from the written table.")
    p(doc, "The costs that the proposal attached to fieldwork were the ones that belong to a participant study: a small token for people who give their time, printing of consent forms and questionnaires, transport inside Kigali, and mobile data where a session could not use the local network. Those items were planned for the evaluation described in Chapter 3. They were not part of the cost of writing the prototype.")

    h(doc, "1.8 Research Timeline", 2)
    p(doc, "The work followed an Agile iterative prototyping cycle over the capstone period. Each pass produced something that could be run, rather than a specification that waited for a final build. The first working piece was the artwork record and the conversion from centimetres to metres. The next was the filter by style, mood, and colour. After that came the reply table, then the ranking after keep, save, or dismiss, then the visit record that is written only when the entrance code is valid, and then the placement of a work at the stored size on a wall. Automated checks were added with each piece, so a later change could be seen against the earlier behaviour.")
    p(doc, "That order mattered. The size rule, the reply, and the ranking do not need a phone in order to be tested, and they did not have to be rewritten when the wall session was connected. The phone session calls the same size rule the editor visit uses. The participant evaluation sat at the end of the plan, after a buyer path existed to evaluate.")

    h(doc, "1.9 Definition of Terms", 2)
    p(doc, "Augmented reality. A view in which a digital object is placed on the live camera image of a real space. In this study it means placing an artwork on a vertical wall at the height and width stored for that work.")
    p(doc, "Artwork visualization. The preview of a work in the buyer’s own space, at the stored size, rather than as a photograph on a page.")
    p(doc, "Conversational decision support. A written reply to a buyer’s question. The buyer types a question. The prototype matches it against a table the researcher wrote. The table covers size, colour, and the story note, in English, French, and Kinyarwanda. It is not a generative model, and it does not learn from the visit.")
    p(doc, "Personalized artwork recommendation. Three other works offered after the buyer keeps, saves, or dismisses the current one. The order follows the style, mood, and colour of the works the buyer has favoured in that visit.")
    p(doc, "Cultural storytelling. The story note stored on the artwork record. It is shown with the work and returned when the buyer asks about the story, the note, or the meaning. It is written in advance. It is not composed at the moment of the question.")
    p(doc, "Feedback-driven refinement. The change in the visit’s weights when the buyer keeps a work, saves it, or dismisses it, and the new set of three works that follows.")
    p(doc, "Buyer uncertainty. Doubt, before purchase, about appearance, scale, spatial fit, suitability, and whether the work matches what the buyer wants (Dimoka et al., 2012).")
    p(doc, "Decision-support prototype. The mobile system that brings the preview, the reply, the story note, and the ranking into one visit.")

    # CHAPTER 2
    h(doc, "CHAPTER TWO: LITERATURE REVIEW", 1)

    h(doc, "2.1 Introduction", 2)
    p(doc, "This chapter is a narrative review, not a systematic review with a PRISMA flow. The search looked in IEEE Xplore, the ACM Digital Library, ScienceDirect, and SpringerLink. Google Scholar was used only to find a peer-reviewed paper that had already been reached from one of those databases. A source was kept when it was in English, peer-reviewed, published from 2019 to 2026, and concerned with one of six themes: augmented-reality systems, augmented reality in online product visualization, recommender systems, conversational decision support, the usability of mobile or augmented-reality applications, and buyer or product uncertainty. The studies below are the ones that remained after that reading. Older works appear only where they are the theory the later studies rely on.")

    h(doc, "2.2 Conceptual Review", 2)
    h(doc, "2.2.1 Augmented reality visualization", 3)
    p(doc, "In this study, augmented reality means a digital artwork placed on the live view of a wall, at the height and width stored for that work. Flavián et al. (2019) linked immersive previews with a richer customer experience. Pfaff and Spann (2025) found the preview especially useful for larger products in the home, which is where doubt about spatial fit is highest. Guo and Zhang (2024) found that an augmented-reality shopping experience shaped perceived usefulness, perceived ease of use, and intention to buy. Together those studies support a true-scale preview for appearance, scale, and fit. They do not study artworks, and they do not change a recommendation after the preview. That second step is what this prototype added.")

    h(doc, "2.2.2 Rule-based conversational decision support", 3)
    p(doc, "A dialogue can draw out what a shopper wants. Liu et al. (2023) and Manzoor et al. (2024) showed that in recommender settings, including settings that use a large language model. This prototype did not implement those models. A generated sentence can sound helpful and still state a size, a colour, or a story that was never stored. For a decision about a physical work, that is a serious kind of help. The prototype used a keyword and rule table the researcher could read and correct. The table answered size, colour, and the story note. When a question matched nothing, the system returned one fixed line and added no fact. The literature justifies having a dialogue step. The build kept that step inspectable.")

    h(doc, "2.2.3 Personalized artwork recommendation", 3)
    p(doc, "Konstan and Riedl (2012) treated the quality of a recommender as a question about the person’s experience, not only about an accuracy score. For art in particular, Yilma and Leiva (2023, 2025) and Yoon and Lee (2024) showed that visual and textual features can improve a recommendation. Those systems train models on large collections. This prototype had a small catalogue prepared for the build, and it did not train a model. Ranking was an overlap of style, mood, and colour, moved up or down by the buyer’s decision to keep, save, or dismiss. The point of that choice was the same one Konstan and Riedl made. The buyer should be able to see why the next three works appeared.")

    h(doc, "2.2.4 Cultural storytelling", 3)
    p(doc, "A work is more than its measurements. De Paolis et al. (2023) and Cecere et al. (2023) showed that a short narrative can be delivered, usably, inside a cultural-heritage system. Those studies are not online purchases. They do support showing meaning alongside dimensions. In this prototype the meaning was a story note stored on the artwork record and returned when the buyer asked about the story, the note, or the meaning. The note was written content. It was not produced when the question arrived.")

    h(doc, "2.2.5 Buyer uncertainty", 3)
    p(doc, "Buyer uncertainty, in this study, is incomplete knowledge before purchase. It covers appearance, scale, spatial fit, suitability, and whether the work lines up with what the buyer wants (Dimoka et al., 2012). Chen et al. (2021) found that ordinary ratings communicate what a product is more readily than they resolve whether it fits. Reduced uncertainty was the primary outcome the study set out to examine. Decision confidence, recommendation relevance, usability, satisfaction, and contextual understanding were supporting readings of that outcome. They were not extra primary aims.")

    h(doc, "2.3 Historical Background of the Research Topic", 2)
    h(doc, "2.3.1 Origins and early context", 3)
    p(doc, "Online art buying grew up with the rest of e-commerce. Through the 2010s, galleries, marketplaces, and auction houses made it possible to find a physical work and pay for it without entering a room. Artsy, founded in 2009, is one marker of that shift: discovery moved online, and the cues that replaced a visit were photographs, metadata, and search. The Hiscox Online Art Trade Report began in 2013 and was still recording that market a decade later (Hiscox, 2013; Hiscox & ArtTactic, 2023). By 2022, online sales of art and collectibles were estimated at $10.8 billion, about 15.9 percent of all art sales.")
    p(doc, "The move changed what a buyer can know before paying. In a gallery the person can judge scale, surface, and the way a piece reads in the light. Online, those cues are pressed into images, a description, and sometimes a short video. Dimoka et al. (2012) described the result as product uncertainty: the buyer cannot properly assess what the product is like, because the seller holds information the buyer does not. For a physical artwork the gap is wider, because the value of the piece depends on the room it will enter. Work on augmented-reality shopping keeps returning to the same complaint. Shoppers cannot touch the product, and they are not given enough of its context, so the purchase disappoints them later (Pfaff & Spann, 2025). That gap, between a picture on a screen and a work on a wall, is the historical root of this study.")

    h(doc, "2.3.2 Key milestones", 3)
    p(doc, "Four milestones matter for the prototype. The first is the online art marketplace itself, visible from about 2009 and written up in the first Hiscox report (Hiscox, 2013). The second is the account of product uncertainty in Dimoka et al. (2012), which later e-commerce work still uses when it needs to explain why a photograph is not an inspection. The third is the arrival of consumer augmented reality. Apple released ARKit with iOS 11 in 2017, and Google announced ARCore 1.0 in 2018 (Apple, 2017; Google, 2018). Retailers then put true-to-scale placement into shopping apps. IKEA Place is the clearest public example: a person could set a piece of furniture into their own room at its real size (Inter IKEA Systems B.V., 2017). The fourth is the recent improvement in conversational recommenders, which became better at drawing out a preference and explaining a suggestion (Liu et al., 2023; Manzoor et al., 2024). This study used that last body of work only as a reason to include a dialogue step. The step itself was a rule table.")

    h(doc, "2.3.3 The gap this study addressed", 3)
    p(doc, "Those advances still tend to arrive separately. A marketplace can recommend. An application can place a work on a wall. A chat can answer. A label can tell a story. A buyer of a physical artwork needs them in one visit, because the doubt is not only about taste and not only about size. It is about taste, size, the room, and the meaning of the piece, held together. The studies reviewed below support each part. They rarely watch what happens when the preview, the reply, the note, and the next recommendation share the same session, and they do not do so for buyers in Kigali. That was the opening for this prototype.")

    h(doc, "2.4 Theoretical Review", 2)
    p(doc, "Three theories were used, each for a different job. The Technology Acceptance Model asks whether a person will find the system useful and easy enough to use. Media Richness Theory asks why a wall preview and a reply carry more of the work than a static page. Uncertainty Reduction Theory asks why seeking that information should leave the buyer less unsure. They are not three names for the same idea.")

    h(doc, "2.4.1 Technology Acceptance Model", 3)
    p(doc, "Davis (1989) explained acceptance through perceived usefulness and perceived ease of use. Usefulness is the belief that the system will help the task. Ease of use is the belief that using it will not be a struggle. Guo and Zhang (2024) carried the model into augmented-reality shopping and found that the experience shaped usefulness, ease of use, and intention to buy. Rejeb et al. (2023), reviewing the retail literature, found the same model still doing most of the explanatory work. For this prototype, the theory was a design constraint. A true-scale preview and a reply only reduce doubt if a buyer can actually use them. The size rule was therefore one rule, shared by the editor and the phone. The reply was a short written answer, not a conversation the buyer had to learn how to steer.")

    h(doc, "2.4.2 Media Richness Theory", 3)
    p(doc, "Daft and Lengel (1986) argued that media differ in how many cues they carry, how quickly they can respond, whether they can use ordinary language, and whether they can attend to a particular person. Richer media are better when the message is ambiguous. Amorim et al. (2022) tested that claim with an augmented-reality shopping assistant in a real shop, and found effects on perceived information quality, on cognitive and emotional response, on brand engagement, and on willingness to buy. The theory is why this study did not stop at a larger photograph. A work at its stored size on the buyer’s wall carries the cue a product page drops. A reply in the buyer’s question carries a response. A story note carries meaning. Each one is a richer channel than the static page, and they were designed to be used together.")

    h(doc, "2.4.3 Uncertainty Reduction Theory", 3)
    p(doc, "Berger and Calabrese (1975) proposed that people try to reduce uncertainty about another person by seeking information, and that more information makes the other person more predictable. The theory began in face-to-face encounters. It has since been used for products a buyer cannot inspect. Hwang and Youn (2023) found that richer, more interactive product information in livestream shopping reduced uncertainty about description, performance, and fit, and raised intention to buy. Medimagh (2025) compared user-generated content and chatbots as two different ways of doing that work. For this study the theory is the link between the four features and the outcome. A buyer who can see the size, ask a question, read the note, and receive three other works has more information than a buyer who has a photograph. Whether that information actually lowers uncertainty for buyers in Kigali is what the evaluation was designed to ask.")

    h(doc, "2.4.4 How the three theories sit together", 3)
    p(doc, "The Technology Acceptance Model is about whether the buyer will use the prototype. Media Richness Theory is about why the preview, the reply, and the note say more than a page. Uncertainty Reduction Theory is about the result those two conditions are supposed to produce. Using all three kept the design from collapsing into a single slogan about engagement. A feature had to be usable, it had to carry a cue the page did not carry, and it had to be the kind of information a buyer seeks when they are unsure.")

    h(doc, "2.5 Overview of Existing Systems", 2)
    p(doc, "Several products already help a person look at art, or at furniture, before buying. They are good at the job they chose. They do not do the whole job this study defined.")
    p(doc, "ArtPlacer places artworks on real walls at their actual dimensions, and it also offers room mockups, virtual exhibitions, and a way to put that view on a website (ArtPlacer, n.d.). A buyer can judge scale and placement. The product is built for presentation and for marketing a work. It does not take a question from a written table, and it does not re-rank other works because the buyer kept or dismissed the one on the wall.")
    p(doc, "Saatchi Art’s View in My Room feature lets a buyer place a work in their own space and judge dimensions, perspective, and the relationship with the room (Saatchi Art, n.d.). That speaks directly to appearance and spatial fit. The preview is not tied to a reply that stays inside stored facts, and it is not tied to a ranking that changes with the buyer’s decision after the preview.")
    p(doc, "Artsy is built around discovery. Recommendations follow what a person has saved, which artists they follow, and how they use the collection tools (Artsy, n.d.). That can make the next work more relevant. It does not place the recommended work on the buyer’s wall at its stored size. A person can find a work they like and still not know whether it fits the room.")
    p(doc, "IKEA Place showed, outside the art market, that true-to-scale placement helps when a product has to live in a particular space, and the wider IKEA tools have added recommendations for furnishing a room (Inter IKEA Systems B.V., 2017). Furniture is not an artwork. The application does not carry a story note about a painting, and it is not a guide through an artwork decision.")
    p(doc, "Read together, these systems show that placement, recommendation, and online discovery already exist. They also show the seam this study worked on. No reviewed product joins a true-scale artwork preview, a reply that cannot invent a fact, a stored story note, and a ranking that moves after the buyer keeps, saves, or dismisses the work. The prototype was built along that seam.")

    h(doc, "2.6 Review of Related Empirical Work", 2)
    p(doc, "The empirical studies from 2019 to 2026 were read for methods and findings, not only for concepts. Artwork and online purchasing came first. Fashion, retail, and cultural heritage were included where the mechanism was the same, for example a product whose fit depends on a body or a room, or a story delivered beside a view.")
    p(doc, "On visualization, Guo and Zhang (2024) surveyed 202 users and found that an augmented-reality shopping experience was positively related to usefulness, ease of use, and intention to buy. Voicu et al. (2023) found that experience with augmented-reality shopping applications was positively related to satisfaction and to the intention to keep using them. Hwang and Youn (2023), with 292 online shoppers, found that richer demonstrations and interactive information reduced product uncertainty, and that the visual demonstration was a strong part of that effect. Their setting was livestream fashion, not art. The transfer is the kind of product: something a still image explains badly. These findings supported placing the artwork at its stored size before the decision.")
    p(doc, "On conversation, Manzoor et al. (2024) compared a large-language-model recommender with a retrieval-based one, with 190 participants. People found the model’s replies more meaningful, and they treated information adequacy and recommendation accuracy as important to the quality of a reply. The study was not about artworks, and it did not place a work in the buyer’s room. It did support letting the buyer ask, rather than only read a label. This prototype took the question and answered it from a table, so that adequacy could be checked against the stored record.")
    p(doc, "On recommendation, Yilma and Leiva (2023) compared multimodal art recommenders with 100 participants and found that representations learned from text and image together did better than either one alone. Their later MOSAIC work extended that line to more than one stakeholder (Yilma & Leiva, 2025). Yoon and Lee (2024) reported a similar gain for MultArtRec, which joins image features and text for artwork recommendation. These studies show that a personalised art recommendation is technically feasible. They judge quality mainly as accuracy. They do not re-rank after the buyer has seen the work on their own wall. That re-ranking was the recommendation this prototype implemented, using style, mood, and colour and the buyer’s keep, save, or dismiss.")
    p(doc, "On storytelling, De Paolis et al. (2023) evaluated a mobile augmented-reality storytelling application for cultural heritage with the System Usability Scale, the User Experience Questionnaire, and the NASA Task Load Index, and found augmented reality a usable way to put a narrative next to a view. Cecere et al. (2023) combined digital storytelling with context-aware recommendation in a heritage setting. Neither study is an online art purchase. Both support the decision to store a story note with the work and to return it when the buyer asks, so that the preview is not the only information in the visit.")
    p(doc, "The pattern across these studies is consistent. Each capability has evidence behind it. The evidence is almost always about one capability, in one domain. Dialogue studies talk about the quality of the reply. Art-recommendation studies talk about accuracy. Storytelling studies talk about engagement and understanding. Very little of it watches one buyer-facing system do all four, and then asks whether the buyer is less unsure about a physical artwork. Evidence of that combination from Rwanda was not found in the review. That absence is a gap in the published record. It is not a finding about Rwandan buyers. The nearest related African evidence is from Ghana, where Amofah and Chai (2022) found that trust mediates e-commerce adoption, and from Rwanda’s own submission to UNCTAD, which names consumer trust as a continuing constraint on digital commerce (UNCTAD, 2024b). Those sources do not say how a person in Kigali would respond to this prototype. They do say that the prototype would be used in a place where trust in buying online is still being built.")

    h(doc, "2.7 Comment on the Literature", 2)
    p(doc, "The review justifies the study on three grounds, and it should not be asked to do more than that. It gives a reason to treat reduced buyer uncertainty as the outcome, with acceptance, media richness, and uncertainty reduction as the lenses. It gives a reason to choose a true-scale preview, a written reply, a recommendation, and a story note as the information the prototype supplies. And it shows that those four are usually studied apart, and not in Kigali. The review does not show that this prototype reduces uncertainty. That claim would need the buyers. What the literature can support, and what the next chapter describes, is the decision to build one visit in which those four kinds of information are available together, and to keep each of them readable.")

    h(doc, "2.8 Conceptual Framework", 2)
    p(doc, "Figure 2.1 links four sources of information to the outcome. Augmented-reality visualization, rule-based conversational support, personalized recommendation, and cultural storytelling each supply something the buyer does not get from a static page. The Technology Acceptance Model explains whether the buyer can use those features. Media Richness Theory explains why they carry more cues than a photograph and a paragraph. Uncertainty Reduction Theory explains the result the study expected, which is lower buyer uncertainty. Decision confidence, recommendation relevance, usability, satisfaction, and contextual understanding sit beside that result as ways of reading it. They are not additional primary outcomes.")
    p(doc, "[Figure 2.1. Four information sources and buyer uncertainty. The figure from the approved proposal still matches this framework and can be placed here.]", first=False, italic=True)

    # CHAPTER 3
    h(doc, "CHAPTER THREE: SYSTEM ANALYSIS AND DESIGN", 1)

    h(doc, "3.1 Introduction", 2)
    p(doc, "This chapter describes how the prototype was analysed, designed, and built. The development method was Agile iterative prototyping. Requirements, a small working piece, a test of that piece, and the next piece followed one another. The method suited the system. The preview, the reply, the story note, and the ranking meet in one visit, and the behaviour of that meeting is easier to see in a running build than in a specification written in advance.")
    p(doc, "The research design was design and evaluation. The prototype was built first. The evaluation with buyers in Kigali was designed around it, as a within-person comparison between a static page and the prototype. This chapter records the design of that evaluation and the system that was built. The tests of the system, and the status of the buyer sessions, are reported in Chapter 4.")

    h(doc, "3.2 Research Design", 2)
    p(doc, "A design-and-evaluation study fitted the questions. The second research question asks how a prototype can carry the preview, the reply, the recommendation, the story note, and the re-ranking as one workflow. That question is answered by a system a person can run. The third research question asks what changes for the buyer. That question needs the same system, used by buyers, with a static page as the comparison.")
    p(doc, "Agile iterative prototyping was the software lifecycle. A cycle ran from a requirement, to a small implementation, to an automated check, and then to the next requirement. Three reasons supported the choice. The parts of the visit affect one another, so they could not be fully specified and then frozen. The buyer’s keep, save, or dismiss is itself an input to the next recommendation, so a feedback-driven cycle matches the product. And the capstone calendar was short. An end-to-end slice that stored a size, answered a question, and ranked three other works could be tested before the wall session was joined to it.")

    h(doc, "3.2.1 The operational catalogue", 3)
    p(doc, "This was a software engineering project, not a machine-learning one. No model was trained, and no labelled dataset was split into training and test sets. The prototype did use an operational catalogue: a small set of artwork records prepared so that the size rule, the filter, the reply, and the ranking could be run and checked.")
    p(doc, "Each record held an identifier, a title, an artist, a year, a medium, the height and the width in centimetres, a style, a mood, a colour, a price band, an image name, and a story note. Height and width had to be greater than zero. A record without an identifier was rejected. An empty story note stayed empty, and the reply then said that no story note was stored. The catalogue was configuration for the prototype. It was not treated as a representative sample of artworks in Kigali, and no statistical claim was made from it.")
    p(doc, "A visit also produced a short record of its own. When the entrance code was valid, the visit could store the code, the question and the reply, and the decision. When the code was not valid, nothing was written. That record is a text file for the visit. It is not an account on a server.")

    h(doc, "3.2.2 The comparison condition", 3)
    p(doc, "The baseline, for a study of this kind, was the ordinary online way of looking at an artwork. The buyer has still images, a written description, and the ability to browse. The buyer does not have the work at its stored size on their own wall, and does not have a reply that is bound to the stored facts. The evaluation was designed as a within-person comparison against that static page, so that each buyer would be their own comparison. The instruments for that comparison are set out in Section 3.2.4. Their results belong in Chapter 4.")

    h(doc, "3.2.3 The workflow that was built", 3)
    p(doc, "The workflow had five stages. They correspond to the four information sources in the framework, with the buyer’s decision feeding the next recommendation.")
    p(doc, "The buyer entered with one of 25 codes. The match ignored case and ignored spaces at the ends of the word. A code the list did not contain did not open a visit record. After a valid code, the menu opened, and from the menu the buyer could start the wall session.")
    p(doc, "Placement used the stored centimetres. The height in metres was the stored height divided by 100, and the width in metres was the stored width divided by 100. A work of 40 centimetres by 50 centimetres therefore occupied 0.40 metres by 0.50 metres. The phone session looked for a vertical plane and placed the work when the buyer tapped that plane. The same conversion was used in the editor visit, so the size on the wall did not depend on which screen had opened the work.")
    p(doc, "The reply was a written table. The buyer’s question was matched, without regard to case, against words for size, colour, and the story. In French the size words included taille, largeur, and hauteur, and the colour word was couleur. In Kinyarwanda the size words included ingano, ubugari, and uburebure, and the colour word was ibara. In English the size words included size, width, height, and centimetre, and the colour words were colour and color. A story question returned the stored note, or a line saying that no note was stored. Any other question returned one fixed line: the system can only answer size, colour, or the story note from the written table. The reply never introduced a number or a colour that the record did not hold.")
    p(doc, "The recommendation used the decision. Keep added 2 to the style, the mood, and the colour of the current work. Save added 1. Dismiss subtracted 1. A blank tag added nothing. The visit then listed three other works, ordered by that score and then by identifier, and it never included the work the buyer had just decided on. The story note of each work remained available with the record.")
    p(doc, "The parts stayed separate in the code, which is what made the iterative build practical. The catalogue knew the record and the filter. The dialogue knew the table. The ranking knew the weights and the three works. The session knew the codes and the visit file. The placement knew how to size a quad from the record. The phone session and the editor visit both asked the placement for the size. None of them had its own copy of the conversion.")

    h(doc, "3.2.4 Measures designed for the evaluation", 3)
    p(doc, "Reduced buyer uncertainty was the primary outcome. The other measures were supporting readings of it. The plan was a within-person comparison. Each buyer would see representative works as a static page, then use the prototype, and the measures would be taken at both points. With a small sample the results were to be described, and reported as paired changes, not as estimates for all buyers in Kigali.")
    p(doc, "The uncertainty score was designed as the mean of five items, each answered from 1 (strongly disagree) to 5 (strongly agree). A lower mean was the better result. The items covered appearance, scale, spatial fit, suitability, and preference alignment, which are the five forms of doubt named in Chapter 1. Decision confidence was to be read the same way, as a paired change. Usability was to be scored with the System Usability Scale, using Brooke’s (1996) method, and the mean compared with the commonly cited value of 68 (Sauro, 2011). Short comments after a session were to be read thematically, alongside the scores, so that a number would not be the only account of what helped.")
    p(doc, "Table 3.1 names those measures and what each one was for. Chapter 4 is where any score would be reported. No score is given in this chapter, because a measure that was designed is not the same thing as a measure that was collected.")
    p(doc, "Table 3.1. Measures designed for the buyer evaluation", first=False, bold=True, space_after=4)
    add_table(
        doc,
        ["Measure", "What it was for", "How it was to be read"],
        [
            ["Buyer uncertainty", "Primary outcome", "Mean of five items, from 1 to 5. A lower mean is better."],
            ["Decision confidence", "Supporting reading", "Paired change, static page then prototype."],
            ["Recommendation relevance", "Supporting reading", "Buyer’s rating of the three other works."],
            ["Usability", "Supporting reading", "System Usability Scale, Brooke (1996)."],
            ["Satisfaction", "Supporting reading", "Rating after the visit."],
            ["Contextual understanding", "Supporting reading", "Whether the story note helped the buyer read the work."],
        ],
    )

    h(doc, "3.2.5 Sampling plan", 3)
    p(doc, "The evaluation was planned with 25 adults, recruited on purpose in Kigali through gallery notices, art-school contacts, and campus networks. The inclusion rules were those in Section 1.5: an adult, a basic comfort with a phone, and an interest in buying online or in buying art. The first eight people were also to be the requirements interviews, with their later evaluation rows marked so that earlier familiarity could be described. Up to three artists or gallery staff were to check the wording of catalogue facts only, not to stand in as buyers. The conduct of that plan is reported with the results.")

    h(doc, "3.2.6 Planned analysis", 3)
    p(doc, "For each buyer the analysis was to compute the five-item uncertainty mean after the static page and after the prototype, then the difference, and then a count of how many scores fell, stayed the same, or rose. Decision confidence was to be treated the same way. The System Usability Scale was to be scored with Brooke’s (1996) method. Given the size of the sample, the report was to stay descriptive.")

    h(doc, "3.3 System Analysis", 2)
    p(doc, "The requirements below are the ones the prototype was built to meet. Each one can be pointed at in the running system or in an automated check. Payment, delivery, auctions, and a public marketplace were left out, as Chapter 1 said they would be.")

    h(doc, "3.3.1 Functional requirements", 3)
    p(doc, "Table 3.2. Functional requirements and where they were met", first=False, bold=True, space_after=4)
    add_table(
        doc,
        ["ID", "Requirement", "What the prototype did"],
        [
            ["FR1", "Store a work with the facts a buyer needs", "The record held identity, artist, year, medium, size, style, mood, colour, price band, and story note."],
            ["FR2", "Reject a work that cannot be placed", "A missing identifier, or a height or width that was not positive, was rejected."],
            ["FR3", "Place the work at its stored size", "Centimetres were divided by 100. The editor visit and the phone session used that result."],
            ["FR4", "Place the work on a wall", "The phone session detected a vertical plane and placed the work when the buyer tapped it."],
            ["FR5", "Find works by style, mood, and colour", "An empty filter left the list unchanged. A tag matched without regard to case."],
            ["FR6", "Answer size, colour, and the story note", "The written table replied in English, French, and Kinyarwanda from the stored record."],
            ["FR7", "Refuse a question the table does not cover", "One fixed line was returned, and no stored fact was added to it."],
            ["FR8", "Rank three other works after a decision", "Keep added 2, Save added 1, and Dismiss subtracted 1. The current work was excluded."],
            ["FR9", "Keep the story note with the work", "An empty note stayed empty. A stored note was returned when the buyer asked."],
            ["FR10", "Open a visit only with a valid code", "Twenty-five codes were accepted. Any other code wrote no visit file."],
        ],
    )

    h(doc, "3.3.2 Non-functional requirements", 3)
    p(doc, "The quality bar was set for a capstone prototype on a phone, not for a public marketplace. Four qualities were designed in, and they can be seen in the build.")
    p(doc, "The replies are auditable. Every answer is a sentence the researcher wrote, filled with a number or a word from the record. A later reader can check a reply against the table and the artwork.")
    p(doc, "The visit does not depend on a network in order to answer or to rank. The records, the table, and the weights are on the device. That also means one buyer’s visit does not change the table for the next buyer. The system is the same at the start of every visit, apart from the decisions taken inside that visit.")
    p(doc, "The size rule is singular. A change to the conversion changes the editor and the phone together, because both call it. The ranking rule is singular in the same way. Keep, save, and dismiss mean the same thing wherever the decision is taken.")
    p(doc, "The automated checks travel with the code. On 13 November 2026 the EditMode suite reported 24 passed and 0 failed. Those checks covered the record, the size conversion, the filter, the three languages and the unknown question, the ranking after keep, save, and dismiss, the visit file, and the quad size. They are the verification of the requirements in Table 3.2. They are not a substitute for a buyer’s score, and they are not presented as one.")

    h(doc, "3.3.3 Architecture", 3)
    p(doc, "The buyer met the system through the phone. Behind that screen sat the artwork records, the wall preview, the reply table, and the visit weights. Those fed two results the buyer could see: three other works, and the story note. Reduced uncertainty sat at the end of that path as the outcome the study hoped for. It was not a module in the software.")
    p(doc, "The code was split the way the visit is split. Capstone.Catalogue held the record, the conversion, and the filter. Capstone.Dialogue held the table and referenced the catalogue. Capstone.Ranking held the decision, the weights, and the choice of three works. Capstone.Session held the codes and the visit file, and it referenced the ranking. Capstone.Placement held the quad sizing and the editor visit, and it referenced the catalogue, the dialogue, and the ranking. The phone session lived with the rest of the application and called the same quad sizing. A test project sat beside each of the first four, so a check could run without starting the phone.")
    p(doc, "[Figure 3.1. Architecture of the prototype. Redraw this from the modules named above. The proposal diagram can guide the layout. The data store in the build is the in-application record, not a separate hosted database.]", first=False, italic=True)

    h(doc, "3.4 System Design", 2)
    p(doc, "The design artefacts below describe the system that was built. They are the data, the buyer’s actions, the classes, and the order of one visit. Where the approved proposal drew a hosted database, this chapter describes the record and the visit file that the prototype actually uses. The behaviour is the same behaviour the proposal asked for. The place the data lives is the application.")

    h(doc, "3.4.1 Artwork record", 3)
    p(doc, "Table 3.3 is the record. It is the whole of the catalogue data the reply, the filter, and the ranking read.")
    p(doc, "Table 3.3. Fields of an artwork record", first=False, bold=True, space_after=4)
    add_table(
        doc,
        ["Field", "Role in the visit"],
        [
            ["Identifier", "Required. Used to find the work and to keep it out of its own recommendation."],
            ["Title, artist, year, medium", "Facts shown with the work."],
            ["Height and width in centimetres", "Required and positive. Divided by 100 for the wall."],
            ["Style, mood, colour", "Used by the filter and by the visit weights."],
            ["Price band", "Stored with the work."],
            ["Image name", "Names the picture associated with the record."],
            ["Story note", "Returned when the buyer asks. Empty stays empty."],
        ],
    )
    p(doc, "The visit file, when the code is valid, is a few lines of text. The first line is the code. A question line holds the question and the reply. A decision line holds keep, save, or dismiss, and the identifier of the work. An invalid code produces no file. That is the session data. It is local to the visit.")

    h(doc, "3.4.2 What the buyer can do", 3)
    p(doc, "The buyer can enter a code, open the wall session, and tap a vertical wall to place the work. In the visit that brings the record onto the screen, the buyer can select a work, read its size in centimetres and in metres, read the story note, ask a question, and keep, save, or dismiss the work. The three other works appear from that decision. The buyer cannot pay, and cannot ask the system to invent a fact about a work.")
    p(doc, "[Figure 3.2. Use of the prototype by the buyer. The actions are entrance, placement, question, and decision.]", first=False, italic=True)

    h(doc, "3.4.3 Classes", 3)
    p(doc, "The classes follow the split in Section 3.3.3. Artwork is the record, and it exposes the size in metres through ArtworkScale. CatalogueFilter selects on style, mood, and colour, and finds a work by identifier. RuleTable answers a question about one artwork. Decision is keep, save, or dismiss. VisitWeights applies that decision to the tags. Ranker returns three other works. EntranceCodes accepts or refuses a code. VisitLog stores the lines of an open visit and writes them only when the visit is open. WorkQuad sets the scale of the placed quad from the artwork. The editor visit and the phone session are the two callers of WorkQuad.")
    p(doc, "[Figure 3.3. Classes of the prototype. Draw the types named in this section and the references between them.]", first=False, italic=True)

    h(doc, "3.4.4 One visit, in order", 3)
    p(doc, "A visit began with the code. If the code was refused, the visit file was not opened and the buyer did not go on. If the code was accepted, the menu was available and the wall session could start. The session looked for a vertical wall. A tap placed the quad at the stored size, a small step off the plane so that it sat on the wall rather than inside it. In the editor visit the same size was applied when a work was chosen, and the facts, the question, and the decision were on the screen with the piece.")
    p(doc, "A question went to the table and the reply came back. A decision updated the weights and the three other works were listed. The current work was not among them. The buyer could place another work and decide again. At the end of a visit that had been opened with a valid code, the lines could be written out. A visit that had never opened wrote nothing.")
    p(doc, "[Figure 3.4. Activity of one visit, from the code to the placement, the question, the decision, and the three other works.]", first=False, italic=True)
    p(doc, "[Figure 3.5. Sequence of one decision. The visit applies the decision to the weights, asks the ranker for three other works, and leaves the current work out.]", first=False, italic=True)

    h(doc, "3.5 Closing Note on the Design", 2)
    p(doc, "The prototype that left this chapter was a single buyer visit. It stored the work in centimetres, placed it at that size, answered from a written table in three languages, kept the story note with the record, and offered three other works after the buyer kept, saved, or dismissed the current one. The parts were separate enough to be tested on their own, and joined enough that the phone and the editor shared the size rule. Chapter 4 turns to the checks that were run on that prototype, and to the buyer evaluation that Chapter 3 designed.")

    h(doc, "References", 1)
    p(doc, "The list below is only the works cited in these three chapters. It follows the author-date form used in the approved proposal. Add any further source when Chapters 4 and 5 cite it. Do not add a source that is not cited.", first=False, italic=True)
    refs = [
        "Amofah, D. O., & Chai, J. (2022). Sustaining consumer e-commerce adoption in Sub-Saharan Africa: Do trust and payment method matter? Sustainability, 14(14), Article 8466. https://doi.org/10.3390/su14148466",
        "Amorim, I. P. de, Guerreiro, J., Eloy, S., & Loureiro, S. M. C. (2022). How augmented reality media richness influences consumer behaviour: A study in a real-world retail environment. International Journal of Consumer Studies, 46(6), 2351–2366. https://doi.org/10.1111/ijcs.12790",
        "Apple. (2017, June 5). iOS 11 brings new features to iPhone and iPad this fall. https://www.apple.com/newsroom/2017/06/ios-11-brings-new-features-to-iphone-and-ipad-this-fall/",
        "ArtPlacer. (n.d.). ArtPlacer. Retrieved October 6, 2026, from https://www.artplacer.com/",
        "Arts Economics. (2025). The Art Basel and UBS global art market report 2025. Art Basel; UBS. https://theartmarket.artbasel.com/download/the-art-basel-and-ubs-art-market-report-2025.pdf",
        "Arts Economics. (2026). The Art Basel and UBS art market report 2026. Art Basel; UBS. https://theartmarket.artbasel.com/download/The-Art-Basel-and-UBS-Art-Market-Report-2026-by-Arts-Economics.pdf",
        "Artsy. (n.d.). About. Retrieved October 6, 2026, from https://www.artsy.net/about",
        "Berger, C. R., & Calabrese, R. J. (1975). Some explorations in initial interaction and beyond: Toward a developmental theory of interpersonal communication. Human Communication Research, 1(2), 99–112. https://doi.org/10.1111/j.1468-2958.1975.tb00258.x",
        "Brooke, J. (1996). SUS: A quick and dirty usability scale. In P. W. Jordan, B. Thomas, I. L. McClelland, & B. Weerdmeester (Eds.), Usability evaluation in industry (pp. 189–194). Taylor & Francis.",
        "Cecere, L., Colace, F., Lombardi, M., Lorusso, A., Santaniello, D., & Valentino, C. (2023). Cultural heritage enhancement through digital storytelling and context-aware recommender system. In Proceedings of the 20th International Conference on Content-Based Multimedia Indexing. Association for Computing Machinery. https://doi.org/10.1145/3617233.3617241",
        "Chen, P.-Y., Hitt, L. M., Hong, Y., & Wu, S. (2021). Measuring product type and purchase uncertainty with online product ratings: A theoretical model and empirical application. Information Systems Research, 32(4), 1470–1489. https://doi.org/10.1287/isre.2021.1041",
        "Daft, R. L., & Lengel, R. H. (1986). Organizational information requirements, media richness and structural design. Management Science, 32(5), 554–571. https://doi.org/10.1287/mnsc.32.5.554",
        "Davis, F. D. (1989). Perceived usefulness, perceived ease of use, and user acceptance of information technology. MIS Quarterly, 13(3), 319–340. https://doi.org/10.2307/249008",
        "De Paolis, L. T., Gatto, C., Corchia, L., & De Luca, V. (2023). Usability, user experience and mental workload in a mobile augmented reality application for digital storytelling in cultural heritage. Virtual Reality, 27(2), 1117–1143. https://doi.org/10.1007/s10055-022-00712-9",
        "Dimoka, A., Hong, Y., & Pavlou, P. A. (2012). On product uncertainty in online markets: Theory and evidence. MIS Quarterly, 36(2), 395–426. https://doi.org/10.2307/41703461",
        "Flavián, C., Ibáñez-Sánchez, S., & Orús, C. (2019). The impact of virtual, augmented and mixed reality technologies on the customer experience. Journal of Business Research, 100, 547–560. https://doi.org/10.1016/j.jbusres.2018.10.050",
        "Google. (2018, February 23). Announcing ARCore 1.0 and new updates to Google Lens. https://blog.google/products-and-platforms/products/google-ar-vr/announcing-arcore-10-and-new-updates-google-lens/",
        "Guo, C., & Zhang, X. (2024). The impact of AR online shopping experience on customer purchase intention: An empirical study based on the TAM model. PLOS ONE, 19(8), Article e0309468. https://doi.org/10.1371/journal.pone.0309468",
        "Hiscox. (2013). The Hiscox online art trade report 2013. https://www.hiscox.co.uk/sites/static-content/online-art-trade-report/docs/hiscox-online-art-trade-report-2013.pdf",
        "Hiscox & ArtTactic. (2023). The Hiscox online art trade report 2023 (10th ed.). https://www.hiscox.co.uk/sites/default/files/documents/2023-04/Hiscox%20online%20art%20trade%20report%202023.pdf",
        "Hwang, J., & Youn, S.-Y. (2023). From brick-and-mortar to livestream shopping: Product information acquisition from the uncertainty reduction perspective. Fashion and Textiles, 10, Article 10. https://doi.org/10.1186/s40691-022-00327-3",
        "Inter IKEA Systems B.V. (2017, September 12). IKEA Place app launched to help people virtually place furniture at home. https://www.ikea.com/global/en/newsroom/innovation/ikea-launches-ikea-place-a-new-app-that-allows-people-to-virtually-place-furniture-in-their-home-170912/",
        "Konstan, J. A., & Riedl, J. (2012). Recommender systems: From algorithms to user experience. User Modeling and User-Adapted Interaction, 22(1–2), 101–123. https://doi.org/10.1007/s11257-011-9118-4",
        "Liu, Y., Zhang, W., Chen, Y., Zhang, Y., Bai, H., Feng, F., Cui, H., Li, Y., & Che, W. (2023). Conversational recommender system and large language model are made for each other in e-commerce pre-sales dialogue. In H. Bouamor, J. Pino, & K. Bali (Eds.), Findings of the Association for Computational Linguistics: EMNLP 2023 (pp. 9587–9605). Association for Computational Linguistics. https://aclanthology.org/2023.findings-emnlp.643/",
        "Manzoor, A., Ziegler, S. C., Pirker Garcia, K. M., & Jannach, D. (2024). ChatGPT as a conversational recommender system: A user-centric analysis. In Proceedings of the 32nd ACM Conference on User Modeling, Adaptation and Personalization (pp. 267–272). Association for Computing Machinery. https://doi.org/10.1145/3627043.3659574",
        "Medimagh, S. (2025). UGC or chatbot? Two strategies, one goal: How UGC and chatbots divergently reduce online product uncertainty. Revue Internationale des Sciences de Gestion, 8(4), 188–213. https://www.revue-isg.com/index.php/home/article/view/2250",
        "Pfaff, A., & Spann, M. (2025). Augmented reality and spatial fit uncertainty in online retailing. Journal of Retailing and Consumer Services, 87, Article 104442. https://doi.org/10.1016/j.jretconser.2025.104442",
        "Rejeb, A., Rejeb, K., & Treiblmaier, H. (2023). How augmented reality impacts retail marketing: A state-of-the-art review from a consumer perspective. Journal of Strategic Marketing, 31(3), 718–748. https://doi.org/10.1080/0965254X.2021.1972439",
        "Saatchi Art. (n.d.). Saatchi Art. Retrieved October 6, 2026, from https://www.saatchiart.com/",
        "Sauro, J. (2011). SUStisfied? Little-known System Usability Scale facts. User Experience Magazine, 10(3). https://uxpamagazine.org/sustified/",
        "United Nations Conference on Trade and Development. (2024a). Business e-commerce sales and the role of online platforms (UNCTAD Technical Notes on ICT for Development No. 1). https://unctad.org/system/files/official-document/dtlecde2024d3_en.pdf",
        "United Nations Conference on Trade and Development. (2024b). Contribution by Rwanda to the 7th Intergovernmental Group of Experts on E-commerce and the Digital Economy. https://unctad.org/system/files/non-official-document/ige7_ecde_c3_Rwanda_en.pdf",
        "Voicu, M.-C., Sîrghi, N., & Toth, D. M.-M. (2023). Consumers’ experience and satisfaction using augmented reality apps in e-shopping: New empirical evidence. Applied Sciences, 13(17), Article 9596. https://doi.org/10.3390/app13179596",
        "Yilma, B. A., & Leiva, L. A. (2023). Together yet apart: Multimodal representation learning for personalised visual art recommendation. In Proceedings of the 31st ACM Conference on User Modeling, Adaptation and Personalization (pp. 204–214). Association for Computing Machinery. https://doi.org/10.1145/3565472.3592964",
        "Yilma, B. A., & Leiva, L. A. (2025). MOSAIC: Multimodal multistakeholder-aware visual art recommendation. User Modeling and User-Adapted Interaction, 35(4), Article 14. https://doi.org/10.1007/s11257-025-09435-3",
        "Yoon, S., & Lee, H. (2024). MultArtRec: A multimodal neural topic modeling for integrating image and text features in artwork recommendation. Electronics, 13(2), Article 302. https://doi.org/10.3390/electronics13020302",
    ]
    for ref in refs:
        para = doc.add_paragraph()
        para.paragraph_format.left_indent = Cm(1.25)
        para.paragraph_format.first_line_indent = Cm(-1.25)
        para.paragraph_format.space_after = Pt(6)
        para.paragraph_format.line_spacing_rule = WD_LINE_SPACING.ONE_POINT_FIVE
        para.alignment = WD_ALIGN_PARAGRAPH.LEFT
        run = para.add_run(ref)
        set_run(run, 12)

    doc.save(OUT)
    print(OUT)


if __name__ == "__main__":
    build()
