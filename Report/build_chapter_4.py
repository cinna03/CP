#!/usr/bin/env python3
"""Chapter 4 from the recorded tests and the prototype. No invented buyer scores."""

from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_LINE_SPACING, WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn

OUT = "/Users/cikirezi/Library/CloudStorage/OneDrive-ThePartnershipAfrica/Documents/GitHub/CP/Report/Mwiza_Cannelle_Capstone_Report_Chapter_4.docx"


def set_run(run, size=12, bold=False, italic=False, name="Times New Roman"):
    run.font.name = name
    run._element.rPr.rFonts.set(qn("w:eastAsia"), name)
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor(0, 0, 0)


def p(doc, text, *, size=12, bold=False, italic=False, center=False, space_after=8, space_before=0, first=True, mono=False):
    para = doc.add_paragraph()
    para.paragraph_format.space_after = Pt(space_after)
    para.paragraph_format.space_before = Pt(space_before)
    para.paragraph_format.line_spacing_rule = WD_LINE_SPACING.ONE_POINT_FIVE
    if center:
        para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    elif mono:
        para.alignment = WD_ALIGN_PARAGRAPH.LEFT
        para.paragraph_format.left_indent = Cm(1.25)
    else:
        para.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        if first:
            para.paragraph_format.first_line_indent = Cm(1.25)
    run = para.add_run(text)
    set_run(run, size, bold, italic, "Consolas" if mono else "Times New Roman")
    return para


def h(doc, text, level):
    para = doc.add_heading(text, level=level)
    for run in para.runs:
        set_run(run, 14 if level == 1 else 13 if level == 2 else 12, bold=True)
    para.paragraph_format.space_before = Pt(16 if level == 1 else 12)
    para.paragraph_format.space_after = Pt(8)


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
    for s in doc.sections:
        s.top_margin = Cm(2.54)
        s.bottom_margin = Cm(2.54)
        s.left_margin = Cm(2.54)
        s.right_margin = Cm(2.54)
    doc.styles["Normal"].font.name = "Times New Roman"
    doc.styles["Normal"].font.size = Pt(12)

    p(doc, "CHAPTER FOUR", size=16, bold=True, center=True, first=False, space_after=4)
    p(doc, "Implementation, Testing, and Discussion", size=14, bold=True, center=True, first=False, space_after=10)
    p(doc, "Insert the Test Runner screenshot where Figure 4.1 is marked, and the editor shots where Figures 4.2 to 4.5 are marked. Insert the iPhone photograph at Figure 4.6 only after the wall check, and write in the note what you actually saw. This chapter does not state a phone pass or fail, and it does not state a buyer score.", first=False, italic=True, space_after=14)

    h(doc, "4.1 Introduction", 2)
    p(doc, "This chapter reports the prototype that was built and the checks that were run on it. The build covered the buyer path set out in Chapter 3: a work stored in centimetres, a wall preview at that size, a reply from a written table, a story note kept with the record, and three other works after the buyer keeps, saves, or dismisses the current one. The checks were automated tests in the Unity editor, plus the same visit exercised on screen.")
    p(doc, "The eight interviews and the comparison with 25 buyers were not conducted. Research questions 1 and 3 therefore have no scores from buyers. Research question 2 is answered here from the prototype and from the tests. The measures designed in Section 3.2.4 remain the plan for a buyer study. They are not reported as results.")

    h(doc, "4.2 Implementation", 2)
    h(doc, "4.2.1 Tools and environment", 3)
    p(doc, "The prototype was built in Unity 6000.5.0f1. Augmented reality used AR Foundation 6.5.0 and ARKit 6.5.0, so a phone session could detect a vertical plane and place a work on it. The interface used the Input System and TextMesh Pro. The automated checks used the Unity Test Framework in EditMode. The development machine was the researcher’s laptop. The phone used for the wall session was an iPhone 13 already owned by the researcher. The artwork records, the reply table, and the ranking lived in the application, so a visit could answer and rank without a network connection.")
    p(doc, "Table 4.1. Tools used to build and check the prototype", first=False, bold=True, space_after=4)
    add_table(
        doc,
        ["Tool", "Role"],
        [
            ["Unity 6000.5.0f1", "Editor, play mode, and the iOS build."],
            ["AR Foundation 6.5.0 and ARKit 6.5.0", "Vertical wall detection and placement on the phone."],
            ["Unity Test Framework, EditMode", "The 24 automated checks."],
            ["GitHub", "Version control of the project."],
            ["iPhone 13", "The device used for the wall session."],
        ],
    )

    h(doc, "4.2.2 The visit as it was built", 3)
    p(doc, "The code was split so that each part of the visit could be tested on its own and then called by the screen. The catalogue held the artwork record, the conversion from centimetres to metres, and the filter. The dialogue held the reply table. The ranking held the decision and the choice of three other works. The session held the 25 entrance codes and the visit file. The placement held the sizing of the quad. The phone session and the editor visit both asked that placement for the size, so the wall and the editor could not drift apart.")
    p(doc, "A buyer entered with a code. Twenty-five codes were accepted, and the match ignored case. Any other code left the visit closed, and a closed visit wrote no file. After a valid code the menu opened. From the menu the wall session looked for a vertical plane. A tap placed the work a small step off that plane. In the editor, choosing a work placed the same quad in front of the camera and showed the title, the centimetres, the metres, and the story note. The buyer could type a question and could keep, save, or dismiss the work. The three other works were listed from that decision, and the current work was not among them.")

    h(doc, "4.2.3 Size", 3)
    p(doc, "The size rule is one line. The metres on the wall are the stored centimetres divided by 100. A work stored as 40 centimetres wide and 50 centimetres tall occupies 0.40 metres by 0.50 metres. A missing identifier is rejected. A height or a width that is zero or negative is rejected. An empty story note is stored as empty, and the reply then says that no story note is stored.")
    p(doc, "The conversion itself is this.", first=False)
    p(doc, "public static float ToMetres(float centimetres)", mono=True, first=False, space_after=0)
    p(doc, "{", mono=True, first=False, space_after=0)
    p(doc, "    return centimetres / 100f;", mono=True, first=False, space_after=0)
    p(doc, "}", mono=True, first=False)
    p(doc, "The quad uses that result for its width and its height. The editor visit and the phone session both call it. A later change to the conversion therefore changes both screens, which is the point of keeping a single rule.")
    p(doc, "quad.localScale = new Vector3(work.WidthMetres, work.HeightMetres, 1f);", mono=True, first=False)

    h(doc, "4.2.4 The reply", 3)
    p(doc, "The reply is a written table, not a model that composes a sentence. The question is matched, without regard to case, against words for size, colour, and the story. English size words include size, width, height, and centimetre. French size words include taille, largeur, and hauteur. Kinyarwanda size words include ingano, ubugari, and uburebure. Colour is matched in each language, and a question about the story, the note, or the meaning returns the stored note. A question that matches nothing returns one fixed line, and that line does not contain the size or the title of the work.")
    p(doc, "public const string FixedLine =", mono=True, first=False, space_after=0)
    p(doc, "    \"I can only answer size, colour, or the story note from the written table.\";", mono=True, first=False)
    p(doc, "A size reply is filled only with the centimetres already on the record. For the sample work used in the editor, 40 centimetres wide and 50 centimetres tall, the English reply is “This work is 40 cm wide and 50 cm tall.” The French reply uses largeur and hauteur. The Kinyarwanda reply uses ubugari and uburebure. The table can be read by a later examiner. It does not add a fact that was not stored.")

    h(doc, "4.2.5 The decision and the three other works", 3)
    p(doc, "Keep, save, and dismiss change the visit’s weights for the style, the mood, and the colour of the current work. Keep adds 2. Save adds 1. Dismiss subtracts 1. A blank tag adds nothing. The next list is three other works, ordered by that score and then by identifier. The work just decided on is left out. Clearing the visit sets the weights back to zero, so one visit does not leak into the next.")
    p(doc, "float change = decision == Decision.Keep ? 2f", mono=True, first=False, space_after=0)
    p(doc, "    : decision == Decision.Save ? 1f", mono=True, first=False, space_after=0)
    p(doc, "    : -1f;", mono=True, first=False)
    p(doc, "On the editor screen, keeping Blue note, whose tags are Quiet, Calm, and Blue, put Linen room first, because that work carries the same three tags. Green study and Red field followed. Blue note itself was not in the list. That is the recommendation a buyer sees after a decision. It is traceable to the tags and to the decision. It is not a trained model.")

    h(doc, "4.3 Testing", 2)
    h(doc, "4.3.1 What was tested, and what was not", 3)
    p(doc, "The automated checks cover the behaviour that can be decided without a buyer in the room. A record either keeps its fields or it does not. A size either converts or it does not. A reply either stays inside the record or it does not. A ranking either excludes the current work or it does not. Those are the checks reported below. They do not measure whether a buyer in Kigali felt less unsure, and they are not presented as that measurement.")
    p(doc, "The checks ran in the Unity Test Runner, in EditMode, on 13 November 2026. The suite contained 24 tests. All 24 passed. None failed. Figure 4.1 is that run.")
    p(doc, "[Figure 4.1. EditMode Test Runner on 13 November 2026, showing 24 passed and 0 failed. Insert 04-editmode-24-passed.png.]", first=False, italic=True)

    h(doc, "4.3.2 Results of the automated checks", 3)
    p(doc, "Table 4.2 groups the 24 tests by the part of the visit they check. The names are the names of the tests.")
    p(doc, "Table 4.2. EditMode results, 13 November 2026", first=False, bold=True, space_after=4)
    add_table(
        doc,
        ["Part", "Tests", "Passed", "What a pass means"],
        [
            ["Catalogue record and size", "6", "6", "Fields are kept. An empty story note stays empty. A missing identifier, a zero height, or a negative width is rejected. Forty by fifty centimetres becomes 0.40 by 0.50 metres."],
            ["Filter", "5", "5", "Style, mood, and colour narrow the list. An empty filter returns every work. An unknown style returns nothing. Find returns the stored note."],
            ["Reply table", "4", "4", "The English reply contains the stored centimetres. The French reply stays in French and contains largeur. The Kinyarwanda reply stays in Kinyarwanda and contains ubugari. An unknown question returns the fixed line and does not contain the size or the title."],
            ["Ranking", "5", "5", "Keep and Save put a matching work first. Dismiss puts that matching work last. The current work is never included. Clearing the visit returns the score to zero."],
            ["Visit file", "3", "3", "There are 25 codes. A valid code records the question and the decision. An invalid code writes nothing and does not create the file."],
            ["Wall size", "1", "1", "The quad’s scale is the stored metres, 0.40 by 0.50 for a work of 40 by 50 centimetres."],
            ["Total", "24", "24", "0 failed."],
        ],
    )
    p(doc, "Two of the edge cases are worth stating in words, because they are the cases a buyer visit is most likely to get wrong. A work with no story note does not receive a generated note. The field stays empty, and the reply says so. A question the table does not recognise, such as a greeting, receives the fixed line and nothing from the catalogue. The system refuses, rather than guessing.")
    p(doc, "The visit file was checked with a temporary file. The code AZURE opened a visit, stored a question line and a decision line, and wrote that text. A code the list did not contain wrote nothing, and the file was not created. That is the access check. The same 25 codes are the codes the entrance screen accepts.")

    h(doc, "4.3.3 The visit on screen", 3)
    p(doc, "The editor visit was used to see the same rules on a screen, with a sample work called Blue note. The record is 40 centimetres wide and 50 centimetres tall, so the wall size is 0.40 metres by 0.50 metres. The story note is “Painted for a small room.” Figure 4.2 is that screen, before a question is typed.")
    p(doc, "[Figure 4.2. Editor visit showing Blue note, 40 cm wide and 50 cm tall, wall size 0.40 m by 0.50 m, and the story note. Insert 05-play-piece.png.]", first=False, italic=True)
    p(doc, "The question “what size is this?” returned “This work is 40 cm wide and 50 cm tall.” Figure 4.3 is that reply. The question “hello” returned the fixed line, and the reply did not contain 40 or Blue. Figure 4.4 is that reply. Keeping Blue note listed three other works: Linen room, then Green study, then Red field. Blue note was not in the list. Figure 4.5 is that decision.")
    p(doc, "[Figure 4.3. Known question. The reply states 40 cm wide and 50 cm tall. Insert 06-play-known-question.png.]", first=False, italic=True)
    p(doc, "[Figure 4.4. Unknown question. The reply is the fixed line. Insert 07-play-unknown-question.png.]", first=False, italic=True)
    p(doc, "[Figure 4.5. Keep on Blue note. The three other works are Linen room, Green study, and Red field. Insert 08-play-keep-three.png.]", first=False, italic=True)
    p(doc, "These four screens are the same rules as the tests, seen together. The tests prove each rule. The screens show that a person can move from the work, to a question, to a decision, without leaving the visit.")

    h(doc, "4.3.4 The phone session", 3)
    p(doc, "The phone build is the entrance, the menu, and the wall session. The practice panel used for Figures 4.2 to 4.5 runs in the editor. On the phone, the buyer enters a code, opens the menu, and starts the augmented-reality session. That session looks for a vertical wall and places one quad when the wall is tapped. The quad is sized by the same conversion as the editor, so the phone does not keep a second copy of the measurements.")
    p(doc, "The device for that session was an iPhone 13. Figure 4.6 is the researcher’s own photograph of one tap on a clear wall, and the note beside it is the researcher’s observation of that tap: the date, the device, Unity 6000.5.0f1, what appeared, and whether the quad stayed. That note is a developer check. It is not a participant result, and it is not a count of buyers.")
    p(doc, "[Figure 4.6. iPhone 13, one tap on a clear vertical wall. Insert 09-phone-wall.png, and copy into the paragraph above only the actual result written in 09-phone-wall.txt. If the note is still blank, leave this figure for the photograph and do not invent the outcome.]", first=False, italic=True)

    h(doc, "4.4 Discussion", 2)
    h(doc, "4.4.1 Research question 1", 3)
    p(doc, "The first question asked which factors make a buyer in Kigali unsure about a physical artwork online, and which requirements follow. The factors used in this study are the ones the proposal took from the literature: appearance, scale, spatial fit, suitability, and preference alignment (Dimoka et al., 2012; Chen et al., 2021; Pfaff & Spann, 2025). The requirements in Table 3.2 were written from those factors. True-scale placement answers scale and spatial fit. The reply and the story note answer appearance and meaning. The ranking after a decision answers preference.")
    p(doc, "What this chapter cannot add is a list of factors spoken by eight interviewees in Kigali. Those interviews were not conducted. The requirements are therefore the requirements of the approved proposal, implemented and checked. They are not a fresh account of what buyers in the city said.")

    h(doc, "4.4.2 Research question 2", 3)
    p(doc, "The second question asked how one prototype could carry a true-scale preview, a rule-based reply, a recommendation, a story note, and a re-ranking as one visit. The build answers that question by keeping one record and letting each part read it. The record stores centimetres, style, mood, colour, and the story note. The wall reads the centimetres. The table reads the centimetres, the colour, and the note. The ranking reads the tags and the decision. The visit file reads the code, the question, and the decision, and it writes them only when the code is valid.")
    p(doc, "The tests are the evidence that this join holds. Twenty-four checks passed and none failed. A 40 by 50 centimetre work became a 0.40 by 0.50 metre quad. The three languages returned the stored size and stayed in their own wording. An unknown question added no fact. Keep and save preferred a matching work, dismiss moved it down, and the current work never reappeared in its own list of three. A valid code wrote the visit. An invalid code wrote nothing. The editor screens showed the same path in one sitting: the piece, the known reply, the refused reply, and the three works after keep.")
    p(doc, "That is a complete answer to the second research question for the prototype that was built. It is an answer about the system. It is not yet an answer about buyers.")

    h(doc, "4.4.3 Research question 3", 3)
    p(doc, "The third question asked what change in uncertainty and decision confidence would be observed after buyers used the prototype, and how they would rate relevance, usability, satisfaction, and contextual understanding. No buyer session was run. There is no five-item uncertainty mean, no paired change from a static page, no System Usability Scale score, and no satisfaction rating. Reporting a number for any of those would invent a result. The question stays open.")
    p(doc, "What can be said, from the system, is what a buyer would have been offered. They would have seen the work at its stored size, they would have received a reply that could be checked against the record, they would have read the story note if one was stored, and they would have been shown three other works after a decision. Whether that offer reduces uncertainty is the evaluation designed in Chapter 3. It is the evaluation this chapter does not have.")

    h(doc, "4.4.4 What the results support", 3)
    p(doc, "The literature in Chapter 2 supported building the four kinds of information, and it warned that they are usually studied apart. The prototype puts them in one visit, and the tests show that each one behaves as specified when they share that visit. Media richness is visible in the difference between a stored photograph and a quad at the stored size, and in a reply that uses the buyer’s question. Uncertainty reduction, as Berger and Calabrese (1975) framed it, would require the buyer to seek that information and to become less unsure. The prototype makes the seeking possible. The tests show that the information returned is the information stored. They stop there, which is as far as a test of the software can honestly go.")
    p(doc, "The practical result is a visit a reviewer can trace. Given a work, the size on the wall is determined. Given a question, the reply is determined. Given a decision, the next three works are determined. Given a code, the visit file is either written or it is not. That trace is the result of this chapter.")

    h(doc, "4.5 Summary", 2)
    p(doc, "The prototype was implemented in Unity, with the catalogue, the reply table, the ranking, and the visit file in the application, and with placement on a vertical wall through AR Foundation. On 13 November 2026, 24 EditMode tests passed and none failed. The editor visit showed Blue note at 40 by 50 centimetres, answered a size question from the record, refused an unknown question, and listed three other works after keep. The phone session uses the same size rule. The interviews and the 25-person comparison were not conducted, so this chapter does not report a change in buyer uncertainty. Chapter 5 draws the conclusion that this evidence will support.")

    doc.save(OUT)
    print(OUT)


if __name__ == "__main__":
    build()
