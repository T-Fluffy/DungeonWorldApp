import fitz  # This is PyMuPDF
import os
import sys

def convert_pdf_to_images(pdf_path, output_folder):
    # Get the name of the book without the .pdf extension to use as a prefix and folder name
    book_name = os.path.splitext(os.path.basename(pdf_path))[0]
    
    # Create a folder named after the PDF file inside the output folder
    pdf_specific_folder = os.path.join(output_folder, book_name)
    
    # Ensure the PDF-specific output folder exists
    os.makedirs(pdf_specific_folder, exist_ok=True)
    
    print(f"Opening {pdf_path}...")
    try:
        pdf_document = fitz.open(pdf_path)
    except Exception as e:
        print(f"Error opening PDF: {e}")
        sys.exit(1)
        
    total_pages = len(pdf_document)
    print(f"Found {total_pages} pages. Starting extraction...")
    print(f"Saving images to: {pdf_specific_folder}")
    
    for page_number in range(total_pages):
        # Load the page
        page = pdf_document.load_page(page_number)
        
        # Set the resolution (dpi=300 is high quality, perfect for the CNN to read text)
        pix = page.get_pixmap(dpi=300)
        
        # Create a clean filename: e.g., The_Warlock_of_Firetop_Mountain_page_001.jpg
        image_filename = f"{book_name}_page_{page_number + 1:03d}.jpg"
        image_filepath = os.path.join(pdf_specific_folder, image_filename)
        
        # Save the image
        pix.save(image_filepath)
        
        # Print progress every 10 pages so you know it hasn't frozen
        if (page_number + 1) % 10 == 0 or (page_number + 1) == total_pages:
            print(f"Saved {page_number + 1}/{total_pages} pages...")
            
    print(f"\nExtraction complete! All images saved to: {pdf_specific_folder}")
    pdf_document.close()

if __name__ == '__main__':
    # You can pass the PDF path via the command line
    if len(sys.argv) < 2:
        print("Usage: python3 pdf_to_images.py <path_to_pdf_file>")
    else:
        target_pdf = sys.argv[1]
        # Base output directory
        output_dir = "dataset/images/train"
        convert_pdf_to_images(target_pdf, output_dir)